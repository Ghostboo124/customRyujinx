using Ryujinx.Common.Logging;
using Ryujinx.SDL2.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using static SDL2.SDL;

namespace Ryujinx.Input.SDL2
{
    public class SDL2GamepadDriver : IGamepadDriver
    {
        private readonly Dictionary<int, string> _gamepadsInstanceIdsMapping;
        private readonly List<string> _gamepadsIds;
        private readonly Lock _lock = new();

        public ReadOnlySpan<string> GamepadsIds
        {
            get
            {
                lock (_lock)
                {
                    return _gamepadsIds.ToArray();
                }
            }
        }

        public string DriverName => "SDL2";

        public event Action<string> OnGamepadConnected;
        public event Action<string> OnGamepadDisconnected;

        public SDL2GamepadDriver()
        {
            _gamepadsInstanceIdsMapping = new Dictionary<int, string>();
            _gamepadsIds = [];

            SDL2Driver.Instance.Initialize();
            SDL2Driver.Instance.OnJoyStickConnected += HandleJoyStickConnected;
            SDL2Driver.Instance.OnJoystickDisconnected += HandleJoyStickDisconnected;
            SDL2Driver.Instance.OnJoyBatteryUpdated += HandleJoyBatteryUpdated;

            // Add already connected gamepads
            int numJoysticks = SDL_NumJoysticks();

            for (int joystickIndex = 0; joystickIndex < numJoysticks; joystickIndex++)
            {
                HandleJoyStickConnected(joystickIndex, SDL_JoystickGetDeviceInstanceID(joystickIndex));
            }
        }

        private string GenerateGamepadId(int joystickIndex)
        {
            Guid guid = SDL_JoystickGetDeviceGUID(joystickIndex);

            // Add a unique identifier to the start of the GUID in case of duplicates.

            if (guid == Guid.Empty)
            {
                return null;
            }

            // Remove the first 4 char of the guid (CRC part) to make it stable
            string guidString = "0000" + guid.ToString().Substring(4);

            string id;

            lock (_lock)
            {
                int guidIndex = 0;
                id = guidIndex + "-" + guidString;

                while (_gamepadsIds.Contains(id))
                {
                    id = (++guidIndex) + "-" + guidString;
                }
            }

            return id;
        }

        private int GetJoystickIndexByGamepadId(string id)
        {
            lock (_lock)
            {
                return _gamepadsIds.IndexOf(id);
            }
        }

        private void HandleJoyStickDisconnected(int joystickInstanceId)
        {
            bool joyConPairDisconnected = false;
            
            if (!_gamepadsInstanceIdsMapping.Remove(joystickInstanceId, out string id))
                return;

            lock (_lock)
            {
                _gamepadsIds.Remove(id);
                if (!SDL2JoyConPair.IsCombinable(_gamepadsIds))
                {
                    _gamepadsIds.Remove(SDL2JoyConPair.Id);
                    joyConPairDisconnected = true;
                }
            }

            OnGamepadDisconnected?.Invoke(id);
            if (joyConPairDisconnected)
            {
                OnGamepadDisconnected?.Invoke(SDL2JoyConPair.Id);
            }
        }

        private void HandleJoyStickConnected(int joystickDeviceId, int joystickInstanceId)
        {
            bool joyConPairConnected = false;
            
            if (SDL_IsGameController(joystickDeviceId) == SDL_bool.SDL_TRUE)
            {
                if (_gamepadsInstanceIdsMapping.ContainsKey(joystickInstanceId))
                {
                    // Sometimes a JoyStick connected event fires after the app starts even though it was connected before
                    // so it is rejected to avoid doubling the entries.
                    return;
                }

                string id = GenerateGamepadId(joystickDeviceId);

                if (id == null)
                {
                    return;
                }

                if (_gamepadsInstanceIdsMapping.TryAdd(joystickInstanceId, id))
                {
                    lock (_lock)
                    {
                        if (joystickDeviceId <= _gamepadsIds.FindLastIndex(_ => true))
                            _gamepadsIds.Insert(joystickDeviceId, id);
                        else
                            _gamepadsIds.Add(id);
                        
                        if (SDL2JoyConPair.IsCombinable(_gamepadsIds))
                        {
                            _gamepadsIds.Remove(SDL2JoyConPair.Id);
                            _gamepadsIds.Add(SDL2JoyConPair.Id);
                            joyConPairConnected = true;
                        }
                    }

                    OnGamepadConnected?.Invoke(id);
                    if (joyConPairConnected)
                    {
                        OnGamepadConnected?.Invoke(SDL2JoyConPair.Id);
                    }
                }
            }
        }
        
        private void HandleJoyBatteryUpdated(int joystickDeviceId, SDL_JoystickPowerLevel powerLevel)
        {
            Logger.Info?.Print(LogClass.Hid,
                $"{SDL_GameControllerNameForIndex(joystickDeviceId)} power level: {powerLevel}");
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SDL2Driver.Instance.OnJoyStickConnected -= HandleJoyStickConnected;
                SDL2Driver.Instance.OnJoystickDisconnected -= HandleJoyStickDisconnected;

                // Simulate a full disconnect when disposing
                foreach (string id in _gamepadsIds)
                {
                    OnGamepadDisconnected?.Invoke(id);
                }

                lock (_lock)
                {
                    _gamepadsIds.Clear();
                }

                SDL2Driver.Instance.Dispose();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        public IGamepad GetGamepad(string id)
        {
            if (id == SDL2JoyConPair.Id)
            {
                lock (_lock)
                {
                    return SDL2JoyConPair.GetGamepad(_gamepadsIds);
                }
            }
            
            int joystickIndex = GetJoystickIndexByGamepadId(id);

            if (joystickIndex == -1)
            {
                return null;
            }

            nint gamepadHandle = SDL_GameControllerOpen(joystickIndex);

            if (gamepadHandle == nint.Zero)
            {
                return null;
            }
            
            if (SDL_GameControllerName(gamepadHandle).StartsWith(SDL2JoyCon.Prefix))
            {
                return new SDL2JoyCon(gamepadHandle, id);    
            }

            return new SDL2Gamepad(gamepadHandle, id);
        }

        public IEnumerable<IGamepad> GetGamepads()
        {
            lock (_gamepadsIds)
            {
                foreach (string gamepadId in _gamepadsIds)
                {
                    yield return GetGamepad(gamepadId);
                }
            }
        }
    }
}
