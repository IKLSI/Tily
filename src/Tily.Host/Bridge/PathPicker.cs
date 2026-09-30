using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Tily.Host.Bridge;

public static class PathPicker
{
    public static async Task<string?> PickAsync(nint windowHandle, string? target)
    {
        if (target == "sound")
        {
            var soundPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.MusicLibrary };
            soundPicker.FileTypeFilter.Add(".wav");
            InitializeWithWindow.Initialize(soundPicker, windowHandle);
            var sound = await soundPicker.PickSingleFileAsync();
            return sound?.Path;
        }

        if (target == "folder")
        {
            var folderPicker = new FolderPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder };
            folderPicker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(folderPicker, windowHandle);
            var picked = await folderPicker.PickSingleFolderAsync();
            return picked?.Path;
        }

        var filePicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder };
        filePicker.FileTypeFilter.Add(".exe");
        filePicker.FileTypeFilter.Add(".cmd");
        filePicker.FileTypeFilter.Add(".bat");
        filePicker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(filePicker, windowHandle);
        var file = await filePicker.PickSingleFileAsync();
        return file?.Path;
    }

    public static async Task<string?> PickJsonAsync(nint windowHandle)
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add(".json");
        InitializeWithWindow.Initialize(picker, windowHandle);
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public static async Task<string?> SaveJsonAsync(nint windowHandle, string suggestedName)
    {
        var picker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary, SuggestedFileName = suggestedName };
        picker.FileTypeChoices.Add("Préférences JSON", new List<string> { ".json" });
        InitializeWithWindow.Initialize(picker, windowHandle);
        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }
}
