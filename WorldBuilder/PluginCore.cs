using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UtilityBelt.Scripting.Interop;
using WorldBuilder.Lib;

namespace WorldBuilder {
    /// <summary>
    /// This is the main plugin class. When your plugin is loaded, Startup() is called, and when it's unloaded Shutdown() is called.
    /// </summary>
    [FriendlyName("WorldBuilder")]
    public class PluginCore : PluginBase {
        private static string? _assemblyDirectory = null;
        private PluginUI UI { get; }

        public Camera Camera { get; }
        public Game Game { get; }
        public Picker Picker { get; }

        public static PluginCore? Instance { get; private set; }

        /// <summary>
        /// Assembly directory containing the plugin dll
        /// </summary>
        public static string AssemblyDirectory {
            get {
                if (_assemblyDirectory == null) {
                    try {
                        _assemblyDirectory = System.IO.Path.GetDirectoryName(typeof(PluginCore).Assembly.Location);
                    }
                    catch {
                        _assemblyDirectory = Environment.CurrentDirectory;
                    }
                }
                return _assemblyDirectory;
            }
            set {
                _assemblyDirectory = value;
            }
        }

        public PluginCore() {
            Instance = this;

            Game = new Game();
            UI = new PluginUI();
            Picker = new Picker();
            Camera = new Camera();
        }

        /// <summary>
        /// Called when your plugin is first loaded.
        /// </summary>
        protected override void Startup() {
            try {

                if (Game.State == UtilityBelt.Scripting.Enums.ClientState.In_Game) {
                    Init();
                }
                else {
                    Game.OnStateChanged += Game_OnStateChanged;
                }
            }
            catch (Exception ex) {
                Log(ex);
            }
        }

        private void Game_OnStateChanged(object sender, UtilityBelt.Scripting.Events.StateChangedEventArgs e) {
            if (e.NewState == UtilityBelt.Scripting.Enums.ClientState.In_Game) {
                Game.OnStateChanged -= Game_OnStateChanged;
                Init();
            }
        }

        private void Init() {
            Game.OnRender3D += Game_OnRender3D;
            Game.OnRender2D += Game_OnRender2D;
        }

        private void Game_OnRender3D(object sender, EventArgs e) {
            try {
                Camera.Update();
            }
            catch (Exception ex) {
                Log(ex);
            }
        }

        private void Game_OnRender2D(object sender, EventArgs e) {
        }

        /// <summary>
        /// Called when your plugin is unloaded. Either when logging out, closing the client, or hot reloading.
        /// </summary>
        protected override void Shutdown() {
            try {
                Game.OnRender3D -= Game_OnRender3D;
                Game.OnRender2D -= Game_OnRender2D;
                Game.OnStateChanged -= Game_OnStateChanged;

                UI.Dispose();

            }
            catch (Exception ex) {
                Log(ex);
            }
        }

        #region logging
        /// <summary>
        /// Log an exception to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="ex"></param>
        internal static void Log(Exception ex) {
            Log(ex.ToString());
        }

        /// <summary>
        /// Log a string to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="message"></param>
        internal static void Log(string message) {
            try {
                File.AppendAllText(System.IO.Path.Combine(AssemblyDirectory, "log.txt"), $"{message}\n");

                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }
        #endregion // logging
    }
}
