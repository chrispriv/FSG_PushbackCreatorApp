(function () {
    const colors = {
        helipad: "#4da3ff",
        tower: "#ef4444",
        windsock: "#eab308",
        parking: "#22c55e"
    };

    const maxMapZoom = 21;
    const moveHitPx = 36;
    const rotateHitPx = 40;

    const imagery = L.tileLayer(
        "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
        {
            maxZoom: maxMapZoom,
            maxNativeZoom: 18,
            attribution: "Tiles © Esri — Esri, Maxar, Earthstar Geographics"
        });
    const streets = L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: maxMapZoom,
        maxNativeZoom: 19,
        attribution: "© OpenStreetMap"
    });

    const androidMap = /Android/i.test(navigator.userAgent);
    const map = L.map("map", {
        center: [20, 0],
        zoom: 2,
        maxZoom: maxMapZoom,
        layers: [imagery],
        zoomControl: true,
        touchZoom: true,
        scrollWheelZoom: !androidMap,
        doubleClickZoom: true,
        boxZoom: false,
        bounceAtZoomLimits: false
    });
    L.control.layers({ Imagery: imagery, Streets: streets }, null, { position: "topright" }).addTo(map);

    window.pcaZoomBy = function (step) {
        const n = Number(step);
        if (!n)
            return;
        const next = Math.max(map.getMinZoom(), Math.min(maxMapZoom, map.getZoom() + n));
        if (next !== map.getZoom())
            map.setZoom(next);
    };

    if (androidMap) {
        let wheelLock = 0;
        window.addEventListener("wheel", function (ev) {
            ev.preventDefault();
            ev.stopPropagation();
            const now = Date.now();
            if (now - wheelLock < 80)
                return;
            wheelLock = now;
            const dy = ev.deltaY !== 0 ? ev.deltaY : ev.deltaX;
            if (!dy)
                return;
            const step = dy > 0 ? -1 : 1;
            const next = Math.max(map.getMinZoom(), Math.min(maxMapZoom, map.getZoom() + step));
            if (next === map.getZoom())
                return;
            if (ev.clientX != null && ev.clientY != null)
                map.setZoomAround(map.mouseEventToLatLng(ev), next);
            else
                map.setZoom(next);
        }, { passive: false, capture: true });
    }

    const overlays = L.layerGroup().addTo(map);
    const preview = L.layerGroup().addTo(map);
    let selectedId = null;
    let applying = false;
    let items = [];
    let airportLocated = false;
    let drag = null;
    let ignoreClick = false;

    function send(payload) {
        if (window.HybridWebView && window.HybridWebView.SendRawMessage)
            window.HybridWebView.SendRawMessage(JSON.stringify(payload));
    }

    function dest(lat, lon, headingDeg, meters) {
        const R = 6378137;
        const rad = Math.PI / 180;
        const lat1 = lat * rad;
        const brng = headingDeg * rad;
        const d = meters / R;
        const lat2 = Math.asin(Math.sin(lat1) * Math.cos(d) + Math.cos(lat1) * Math.sin(d) * Math.cos(brng));
        const lon2 = lon * rad + Math.atan2(
            Math.sin(brng) * Math.sin(d) * Math.cos(lat1),
            Math.cos(d) - Math.sin(lat1) * Math.sin(lat2));
        return [lat2 / rad, lon2 / rad];
    }

    function bearing(lat1, lon1, lat2, lon2) {
        const r = Math.PI / 180;
        const φ1 = lat1 * r;
        const φ2 = lat2 * r;
        const Δλ = (lon2 - lon1) * r;
        const y = Math.sin(Δλ) * Math.cos(φ2);
        const x = Math.cos(φ1) * Math.sin(φ2) - Math.sin(φ1) * Math.cos(φ2) * Math.cos(Δλ);
        return (Math.atan2(y, x) / r + 360) % 360;
    }

    function rectangleLatLngs(lat, lon, heading, lengthM, widthM) {
        const hl = lengthM / 2;
        const hw = widthM / 2;
        const front = dest(lat, lon, heading, hl);
        const back = dest(lat, lon, heading, -hl);
        return [
            dest(front[0], front[1], heading + 90, hw),
            dest(front[0], front[1], heading + 90, -hw),
            dest(back[0], back[1], heading + 90, -hw),
            dest(back[0], back[1], heading + 90, hw)
        ];
    }

    function headingLength(item) {
        if (item.kind === "helipad" && item.radiusM > 0)
            return Math.max(item.radiusM * 1.55, 28);
        const lengthM = item.lengthM > 0 ? item.lengthM : 12;
        return Math.max(lengthM * 0.85, 28);
    }

    function kindColor(kind) {
        return colors[kind] || "#eeeeee";
    }

    function canRotate(item) {
        return item.kind === "helipad" || item.kind === "parking" || item.kind === "windsock";
    }

    function findItem(id) {
        for (let i = 0; i < items.length; i++) {
            if (items[i].id === id)
                return items[i];
        }
        return null;
    }

    function freezeMapPan() {
        map.dragging.disable();
        if (map.touchZoom)
            map.touchZoom.disable();
    }

    function unfreezeMapPan() {
        map.dragging.enable();
        if (map.touchZoom)
            map.touchZoom.enable();
    }

    function tagLayer(layer, item, role) {
        const apply = function () {
            const el = layer.getElement && layer.getElement();
            if (!el)
                return;
            el.setAttribute("data-pca-id", item.id);
            el.setAttribute("data-pca-role", role);
        };
        layer.on("add", apply);
        overlays.addLayer(layer);
        apply();
    }

    map.getContainer().addEventListener("pointerdown", function (ev) {
        if (ev.button != null && ev.button !== 0)
            return;
        const hit = ev.target && ev.target.closest && ev.target.closest("[data-pca-id]");
        if (!hit)
            return;
        const item = findItem(hit.getAttribute("data-pca-id"));
        if (!item)
            return;
        ev.preventDefault();
        ev.stopPropagation();
        freezeMapPan();
        selectedId = item.id;
        drag = {
            mode: hit.getAttribute("data-pca-role") === "rotate" ? "rotate" : "move",
            item: item,
            moved: false,
            startClientX: ev.clientX,
            startClientY: ev.clientY
        };
        drawPreview();
    }, true);

    function drawOverlays() {
        overlays.clearLayers();
            items.forEach(function (item) {
            if (item.lat == null || item.lon == null)
                return;
            if (drag && drag.item && item.id === drag.item.id)
                return;
            const lat = item.lat;
            const lon = item.lon;
            const selected = item.id === selectedId;
            const color = kindColor(item.kind);
            const heading = item.heading || 0;
            const arrowM = headingLength(item);
            const tip = dest(lat, lon, heading, arrowM);

            if (item.kind === "tower") {
                const halo = L.circleMarker([lat, lon], {
                    radius: 16,
                    color: selected ? "#ffffff" : color,
                    weight: 2,
                    fillColor: color,
                    fillOpacity: selected ? 0.35 : 0.18
                });
                tagLayer(halo, item, "move");
            } else {
                let shape;
                if (item.kind === "helipad" && item.radiusM > 0) {
                    shape = L.circle([lat, lon], {
                        radius: item.radiusM,
                        color: selected ? "#ffffff" : color,
                        weight: selected ? 3 : 2,
                        fillColor: color,
                        fillOpacity: selected ? 0.28 : 0.16
                    });
                } else {
                    const lengthM = item.lengthM > 0 ? item.lengthM : 12;
                    const widthM = item.widthM > 0 ? item.widthM : 8;
                    shape = L.polygon(rectangleLatLngs(lat, lon, heading, lengthM, widthM), {
                        color: selected ? "#ffffff" : color,
                        weight: selected ? 3 : 2,
                        fillColor: color,
                        fillOpacity: selected ? 0.32 : 0.18
                    });
                }
                tagLayer(shape, item, "move");

                const left = dest(tip[0], tip[1], heading + 150, Math.max(4, arrowM * 0.22));
                const right = dest(tip[0], tip[1], heading - 150, Math.max(4, arrowM * 0.22));
                const arrow = L.polyline([[lat, lon], tip, left, tip, right], {
                    color: selected ? "#ffffff" : color,
                    weight: selected ? 3 : 2,
                    opacity: 0.95
                });
                tagLayer(arrow, item, "move");
            }

            const moveIcon = L.marker([lat, lon], {
                zIndexOffset: selected ? 700 : 400,
                keyboard: false,
                icon: L.divIcon({
                    className: "handle-move",
                    iconSize: [moveHitPx, moveHitPx],
                    iconAnchor: [moveHitPx / 2, moveHitPx / 2],
                    html: `<div class="handle-move-hit" data-pca-id="${item.id}" data-pca-role="move"><div class="handle-move-dot" style="background:${selected ? "#ffffff" : color};"></div></div>`
                })
            });
            moveIcon.bindTooltip(item.label || item.kind, {
                permanent: true,
                direction: "right",
                offset: [12, 0],
                className: "object-label",
                opacity: 0.92,
                interactive: false
            });
            tagLayer(moveIcon, item, "move");

            if (selected && canRotate(item)) {
                const rotateIcon = L.marker(tip, {
                    zIndexOffset: 800,
                    keyboard: false,
                    icon: L.divIcon({
                        className: "handle-rotate",
                        iconSize: [rotateHitPx, rotateHitPx],
                        iconAnchor: [rotateHitPx / 2, rotateHitPx / 2],
                        html: `<div class="handle-rotate-hit" data-pca-id="${item.id}" data-pca-role="rotate"><div class="handle-rotate-dot"></div></div>`
                    })
                });
                rotateIcon.bindTooltip("Drag to set heading", {
                    direction: "top",
                    offset: [0, -8],
                    className: "object-label",
                    opacity: 0.92,
                    interactive: false
                });
                tagLayer(rotateIcon, item, "rotate");
            }
        });
    }

    window.applyScene = function applyScene(scene) {
        applying = true;
        try {
            selectedId = scene && scene.selectedId ? scene.selectedId : null;
            airportLocated = !!(scene && scene.airportLocated);
            items = (scene && scene.items) ? scene.items : [];
            if (!drag)
                preview.clearLayers();
            if (drag && drag.item && drag.item.id)
            {
                const live = findItem(drag.item.id);
                if (live)
                    drag.item = live;
            }
            drawOverlays();
            const bounds = [];
            items.forEach(function (item) {
                if (item.lat != null && item.lon != null)
                    bounds.push([item.lat, item.lon]);
            });

            if (!drag && scene && scene.view && scene.view.lat != null && scene.view.lon != null) {
                if (scene.view.panOnly)
                    map.panTo([scene.view.lat, scene.view.lon]);
                else
                    map.setView([scene.view.lat, scene.view.lon], scene.view.zoom || 16);
            } else if (!drag && scene && scene.fit && bounds.length > 0) {
                if (bounds.length === 1)
                    map.setView(bounds[0], 19);
                else
                    map.fitBounds(bounds, { padding: [28, 28], maxZoom: maxMapZoom });
            }
        } finally {
            applying = false;
        }
        return true;
    };

    function drawPreview() {
        preview.clearLayers();
        if (!drag || !drag.item || drag.item.lat == null || drag.item.lon == null)
            return;
        const item = drag.item;
        const lat = item.lat;
        const lon = item.lon;
        const heading = item.heading || 0;
        const color = kindColor(item.kind);
        const arrowM = headingLength(item);
        const tip = dest(lat, lon, heading, arrowM);
        if (item.kind === "tower") {
            preview.addLayer(L.circleMarker([lat, lon], {
                radius: 16,
                color: "#ffffff",
                weight: 2,
                fillColor: color,
                fillOpacity: 0.45
            }));
            return;
        }
        if (item.kind === "helipad" && item.radiusM > 0) {
            preview.addLayer(L.circle([lat, lon], {
                radius: item.radiusM,
                color: "#ffffff",
                weight: 3,
                fillColor: color,
                fillOpacity: 0.28
            }));
        } else {
            const lengthM = item.lengthM > 0 ? item.lengthM : 12;
            const widthM = item.widthM > 0 ? item.widthM : 8;
            preview.addLayer(L.polygon(rectangleLatLngs(lat, lon, heading, lengthM, widthM), {
                color: "#ffffff",
                weight: 3,
                fillColor: color,
                fillOpacity: 0.32
            }));
        }
        const left = dest(tip[0], tip[1], heading + 150, Math.max(4, arrowM * 0.22));
        const right = dest(tip[0], tip[1], heading - 150, Math.max(4, arrowM * 0.22));
        preview.addLayer(L.polyline([[lat, lon], tip, left, tip, right], {
            color: "#ffffff",
            weight: 3,
            opacity: 0.95
        }));
    }

    function mapPoint(ev) {
        if (ev.latlng)
            return ev.latlng;
        if (ev.clientX != null) {
            const rect = map.getContainer().getBoundingClientRect();
            return map.containerPointToLatLng(L.point(ev.clientX - rect.left, ev.clientY - rect.top));
        }
        return null;
    }

    function onPointerMove(e) {
        if (!drag)
            return;
        const ll = mapPoint(e);
        if (!ll)
            return;
        if (e.containerPoint && drag.startX != null) {
            const dx = e.containerPoint.x - drag.startX;
            const dy = e.containerPoint.y - drag.startY;
            if (dx * dx + dy * dy > 9)
                drag.moved = true;
        } else {
            drag.moved = true;
        }
        if (drag.mode === "move") {
            drag.item.lat = ll.lat;
            drag.item.lon = ll.lng;
        } else {
            drag.item.heading = bearing(drag.item.lat, drag.item.lon, ll.lat, ll.lng);
        }
        drawPreview();
    }

    function onPointerUp() {
        unfreezeMapPan();
        if (!drag)
            return;
        const d = drag;
        drag = null;
        preview.clearLayers();
        drawOverlays();
        ignoreClick = true;
        setTimeout(function () { ignoreClick = false; }, 400);
        if (!d.moved) {
            send({ type: "select", id: d.item.id });
            return;
        }
        if (d.mode === "move") {
            send({ type: "moved", id: d.item.id, lat: d.item.lat, lon: d.item.lon });
        } else {
            send({ type: "heading", id: d.item.id, heading: d.item.heading });
        }
    }

    function onWindowPointerMove(ev) {
        if (!drag)
            return;
        const ll = mapPoint(ev);
        if (!ll)
            return;
        const dx = ev.clientX - (drag.startClientX || 0);
        const dy = ev.clientY - (drag.startClientY || 0);
        if (dx * dx + dy * dy > 9)
            drag.moved = true;
        if (!drag.moved)
            return;
        if (drag.mode === "move") {
            drag.item.lat = ll.lat;
            drag.item.lon = ll.lng;
        } else {
            drag.item.heading = bearing(drag.item.lat, drag.item.lon, ll.lat, ll.lng);
        }
        drawPreview();
    }

    map.on("mousemove", onPointerMove);
    map.on("mouseup", onPointerUp);
    window.addEventListener("pointermove", onWindowPointerMove, true);
    window.addEventListener("pointerup", onPointerUp, true);
    window.addEventListener("pointercancel", onPointerUp, true);
    window.addEventListener("mouseup", onPointerUp, true);

    map.on("click", function (e) {
        if (applying || ignoreClick || drag)
            return;
        if (!airportLocated) {
            send({ type: "mapclick", id: "helipad", lat: e.latlng.lat, lon: e.latlng.lng });
            return;
        }
        const selected = findItem(selectedId);
        if (selected && (selected.lat == null || selected.lon == null)) {
            send({ type: "mapclick", id: selected.id, lat: e.latlng.lat, lon: e.latlng.lng });
            return;
        }
        selectedId = null;
        drawOverlays();
        send({ type: "deselect" });
    });

    send({ type: "ready" });
})();
