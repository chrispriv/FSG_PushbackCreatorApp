namespace FSG_PushbackCreator.Services;

public static class UnsavedChanges
{
	public static Task<bool> ConfirmDiscardPageAsync(Page page) =>
		page.DisplayAlert(
			"Unsaved changes",
			"You have unsaved changes. Discard them?",
			"Discard",
			"Keep editing");

	public static Task<bool> ConfirmCloseProjectAsync(Page page) =>
		page.DisplayAlert(
			"Unsaved changes",
			"This project has changes that were not exported. Close the project anyway?",
			"Close project",
			"Keep editing");
}
