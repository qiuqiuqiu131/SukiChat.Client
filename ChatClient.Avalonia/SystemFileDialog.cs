namespace ChatClient.Avalonia;

using System;
using System.Runtime.InteropServices;
using PInvoke;

public class SystemFileDialog
{
    [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

    [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class OpenFileName
    {
        public int lStructSize = Marshal.SizeOf(typeof(OpenFileName));
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile = 256;
        public string lpstrFileTitle;
        public int nMaxFileTitle = 64;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    public static Task<string> OpenFileAsync(IntPtr ownerHandle, string windowName = "Open FIle", string filter = "Image Files\0*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.tiff;*.webp\0All Files\0*.*\0", string initialDir = "")
    {
        var tcs = new TaskCompletionSource<string>();

        Task.Run(() =>
        {
            OpenFileName ofn = new OpenFileName
            {
                hwndOwner = ownerHandle,
                lpstrFilter = filter,
                lpstrFile = new string(new char[256]),
                lpstrFileTitle = new string(new char[64]),
                lpstrInitialDir = initialDir,
                lpstrTitle = windowName,
                Flags = 0x00000008 | 0x00080000 | 0x00000002 // OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST | OFN_EXPLORER
            };

            if (GetOpenFileName(ofn))
            {
                tcs.SetResult(ofn.lpstrFile);
            }
            else
            {
                tcs.SetResult(null);
            }
        });

        return tcs.Task;
    }

    public static Task<string> SaveFileAsync(
    IntPtr ownerHandle,
    string defaultFileName = "",
    string windowName = "Save File",
    string filter = "All Files\0*.*\0",
    string initialDir = "")
    {
        var tcs = new TaskCompletionSource<string>();

        Task.Run(() =>
        {
            OpenFileName ofn = new OpenFileName
            {
                hwndOwner = ownerHandle,
                lpstrFilter = filter,
                lpstrFile = string.IsNullOrEmpty(defaultFileName)
                    ? new string(new char[256])
                    : defaultFileName.PadRight(256, '\0'),
                lpstrFileTitle = new string(new char[64]),
                lpstrInitialDir = initialDir,
                lpstrTitle = windowName,
                Flags = 0x00000002 | 0x00000004  // OFN_EXPLORER | OFN_OVERWRITEPROMPT
            };

            if (GetSaveFileName(ofn))
            {
                tcs.SetResult(ofn.lpstrFile);
            }
            else
            {
                tcs.SetResult(null);
            }
        });

        return tcs.Task;
    }
}