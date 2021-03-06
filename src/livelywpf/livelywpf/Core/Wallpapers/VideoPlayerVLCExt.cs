﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace livelywpf.Core
{
    /// <summary>
    /// libVLC videoplayer (External plugin.)
    /// </summary>
    public class VideoPlayerVLCExt : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        IntPtr HWND { get; set; }
        Process Proc { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }
        /// <summary>
        /// copy of LivelyProperties.json file used to modify for current running screen.
        /// </summary>
        //string LivelyPropertyCopy { get; set; }
        private bool Initialized { get; set; }
        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public VideoPlayerVLCExt(string path, LibraryModel model, LivelyScreen display)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = "\"" + path + "\"",
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer", "libVLCPlayer.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer")
            };

            Process videoPlayerProc = new Process
            {
                StartInfo = start,
                EnableRaisingEvents = true
            };
            //webProcess.OutputDataReceived += WebProcess_OutputDataReceived;

            this.Proc = videoPlayerProc;
            this.Model = model;
            this.Display = display;
        }

        public void Close()
        {
            try
            {
                Proc.Refresh();
                Proc.StandardInput.WriteLine("lively:terminate");
            }
            catch
            {
                Terminate();
            }
        }

        public IntPtr GetHWND()
        {
            return HWND;
        }

        public Process GetProcess()
        {
            return Proc;
        }

        public LivelyScreen GetScreen()
        {
            return Display;
        }

        public LibraryModel GetWallpaperData()
        {
            return Model;
        }

        public WallpaperType GetWallpaperType()
        {
            return Model.LivelyInfo.Type;
        }

        public void Pause()
        {
            SendMessage("lively:vid-pause");
        }

        public void Play()
        {
            SendMessage("lively:vid-play");
        }

        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
        }

        public void Show()
        {
            if (Proc != null)
            {
                try
                {
                    Proc.Exited += Proc_Exited;
                    Proc.OutputDataReceived += Proc_OutputDataReceived;
                    Proc.Start();
                    Proc.BeginOutputReadLine();
                }
                catch (Exception e)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e, Msg = "Failed to start process." });
                    Close();
                }
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            if (!Initialized)
            {
                //Exited with no error and without even firing OutputDataReceived; probably some external factor.
                WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                {
                    Success = false,
                    Error = new Exception(Properties.Resources.LivelyExceptionGeneral),
                    Msg = "Process exited before giving HWND."
                });
            }
            Proc.OutputDataReceived -= Proc_OutputDataReceived;
            Proc.Dispose();
            SetupDesktop.RefreshDesktop();
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //When the redirected stream is closed, a null line is sent to the event handler.
            if (!String.IsNullOrEmpty(e.Data))
            {
                if (e.Data.Contains("HWND"))
                {
                    bool status = true;
                    Exception error = null;
                    string msg = null;
                    try
                    {
                        msg = e.Data;
                        IntPtr handle = new IntPtr();
                        handle = new IntPtr(Convert.ToInt32(e.Data.Substring(4), 10));
                        if (IntPtr.Equals(handle, IntPtr.Zero))//unlikely.
                        {
                            status = false;
                        }
                        SetHWND(handle);
                    }
                    catch (Exception ex)
                    {
                        status = false;
                        error = ex;
                    }
                    finally
                    {
                        if (!Initialized)
                        {
                            WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = status, Error = error, Msg = msg });
                        }
                        //First run sent msg will be window handle.
                        Initialized = true;
                    }
                }
                Logger.Info("libVLC(Ext):" + e.Data);
            }
        }

        public void Stop()
        {
            //throw new NotImplementedException();
        }

        public void SendMessage(string msg)
        {
            if (Proc != null)
            {
                try
                {
                    Proc.StandardInput.WriteLine(msg);
                }
                catch { }
            }
        }

        public string GetLivelyPropertyCopyPath()
        {
            return null;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public void Terminate()
        {
            try
            {
                Proc.Kill();
                Proc.Dispose();
            }
            catch { }
            SetupDesktop.RefreshDesktop();
        }

        public void SetVolume(int volume)
        {
            SendMessage("lively:vid-volume " + volume);
        }

        public void SetPlaybackPos(int pos)
        {
            //todo
        }
    }
}
