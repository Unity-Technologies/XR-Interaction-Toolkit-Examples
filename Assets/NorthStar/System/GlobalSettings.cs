// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using Meta.Utilities;
using Oculus.Interaction.GrabAPI;
using UnityEngine;
using UnityEngine.Audio;

namespace NorthStar
{
    /// <summary>
    /// Scriptable object for handling setting variables from a consistent source
    /// </summary>
    [CreateAssetMenu(menuName = "Data/Global Settings")]
    public class GlobalSettings : ScriptableObject
    {
        private const string ASSETNAME = "GlobalSettings";
        private static GlobalSettings s_instance;
        public static GlobalSettings Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = Resources.Load(ASSETNAME) as GlobalSettings;
                    s_instance.Setup();
                }
                return s_instance;
            }
            private set { }
        }

        [Serializable]
        public class GlobalPlayerSettings
        {
            public bool Seated { get => PlayerConfigs.Seated; set => PlayerConfigs.Seated = value; }
            public float Height { get => PlayerConfigs.Height; set => PlayerConfigs.Height = value; }
            public float SeatedHeight { get => PlayerConfigs.SeatedHeight; set => PlayerConfigs.SeatedHeight = value; }

            [Header("Calibration"), SerializeField]
            public PlayerConfigs PlayerConfigs = new()
            {
                Seated = false,
                Height = 175,
                SeatedHeight = 92.5f,
                ComfortLevel = ComfortLevel.Off,
                MuteMusic = false,
                MuteVoices = false,
                DisableCaptions = false,
            };

            public float PlayerHeight => Seated ? SeatedHeight : Height;
            public event Action OnPlayerCalibrationChange;
            public void PlayerCalibrationChanged()
            {
                OnPlayerCalibrationChange?.Invoke();
            }

            public ComfortLevel ComfortLevel { get => PlayerConfigs.ComfortLevel; set => PlayerConfigs.ComfortLevel = value; }
            [Header("Comfort")]

            public ComfortPreset[] ComfortPresets = new ComfortPreset[4];
            public float ReorientStrength { get => PlayerConfigs.ReorientPlayer; set => PlayerConfigs.ReorientPlayer = value; }

            public event Action<ComfortLevel> OnComfortLevelChanged;
            public void ComfortLevelChanged()
            {
                OnComfortLevelChanged?.Invoke(PlayerConfigs.ComfortLevel);
                var comfortPreset = ComfortPresets[(int)ComfortLevel];
                BoatMovementStrength = comfortPreset.BoatMovementStrength;
                BoatReactionStrength = comfortPreset.BoatMovementStrength;
            }

            [Header("Movement")]
            public bool MovementEnabled = true;
            public float PlayerMovementSpring = 1000;
            public float PlayerMovementDamper = 150;
            public float PlayerMovementMaxForce = 1000;
            [Header("Teleporter")]
            public float TeleportHandMovementTriggerDistance = 0.25f;
            public float TeleportHandTriggerDelay = 0.5f;
            public float TeleporterToCloseDespawnDistance = 2;
            public bool TeleportRotationRotatesHead = false;
            public bool TeleportCenteredOnHead = false;
            [Range(0f, 1f)] public float TeleporterHeadAngleMin;
            [Range(0f, 1f)] public float TeleporterArmAngleMin;

            [Range(0f, 1f)] public float TeleporterHeadAngleBreak;
            [Range(0f, 1f)] public float TeleporterArmAngleBreak;
            [Range(0f, 1f)] public float TeleporterPalmAngleBreak;
            public LayerMask TeleporterLayers;

            [Header("Hands")]
            public GrabbingRule DefaultPalmGrabRule = new();
            public float HandMovementStrength = 1000;
            [Range(0, 2)] public float HandsMovementSpring = 1;
            [Range(0, 2)] public float HandsMovementDamper = .1f;
            [Range(0, 2)] public float HandsMovementMaxForce = 2;
            public bool HandsUseAccelerationMovement = false;
            [Space]
            public float HandRotationStrength = 1000;
            [Range(0, 10)] public float HandsRotationSpring = 1;
            [Range(0, 10)] public float HandsRotationDamper = .1f;
            [Range(0, 10)] public float HandsRotationMaxForce = 2;
            public bool HandsUseAccelerationRotation = false;
            public bool HandsReleaseOnInvalidPosition = true;
            public bool AllowForcedHandBreak = true;
            [Space]
            public float MaxHandDistance = .5f;
            [Space]
            public Vector3 WristLowerAngularLimits;
            public Vector3 WristUpperAngularLimits;
            public float HandBreakTimeout = .1f;
            public float ArmStretchLimit;
            public bool ResetBodyTrackingOnWake = true;
            [Range(0, 1)] public float BoatMovementStrength = 1.0f;
            [Range(0, 1)] public float BoatReactionStrength = 1.0f;
            [SerializeField] private AudioMixer m_audioMixer;
            public bool MusicMuted
            {
                get => PlayerConfigs.MuteMusic;
                set
                {
                    PlayerConfigs.MuteMusic = value;
                    _ = m_audioMixer.SetFloat("MusicVolume", value ? -80 : 0);
                }

            }
            public bool VoiceMuted
            {
                get => PlayerConfigs.MuteVoices;
                set
                {
                    PlayerConfigs.MuteVoices = value;
                    _ = m_audioMixer.SetFloat("VoiceVolume", value ? -80 : 0);
                }
            }
            public bool DisableCaptions { get => PlayerConfigs.DisableCaptions; set => PlayerConfigs.DisableCaptions = value; }
        }

        [Serializable]
        public class GlobalScreenSettings
        {
            public Vector2 ScreenMinBounds = Vector2.zero;
            public Vector2 ScreenMaxBounds = Vector2.one;
            public float ScreenRadius = 0.5f;

            [Space]
            public float TextFadeTime = 0.1f;
            public float TextShowTime = 3f;
            public float SubtitleUpOffset = 1;
        }

        [Serializable]
        public class GlobalFtueSettings
        {
            public float PulseDelay = .5f;
            public float PulseStrength;
            public Color PulseTint;
            public float PulseGlowPower;
            public float PulseSpeed = 1;
            public AnimationCurve EasingCurve;
            public float FadeInTime = .1f;

            private const string PULSE_STRENGTH_KEY = "_Pulse_Strength";
            private const string PULSE_TINT_KEY = "_Pulse_Tint";
            private const string PULSE_GLOW_KEY = "_Pulse_Glow_Power";

            public Action OnValidate;
            public void SetupMaterialForFtue(Material material)
            {
                material.SetFloat(PULSE_STRENGTH_KEY, PulseStrength);
                material.SetColor(PULSE_TINT_KEY, PulseTint);
                material.SetFloat(PULSE_GLOW_KEY, PulseGlowPower);
            }

            public float GetPulseValue()
            {
                return EasingCurve.Evaluate(Mathf.Sin(Time.time * PulseSpeed).Map(-1, 1, 0, 1));
            }
        }

        [SerializeField] private GlobalPlayerSettings m_playerSettings;
        [SerializeField] private GlobalScreenSettings m_screenSettings;
        [SerializeField] private GlobalFtueSettings m_ftueSettings;

        public static GlobalPlayerSettings PlayerSettings => Instance.m_playerSettings;
        public static GlobalScreenSettings ScreenSettings => Instance.m_screenSettings;
        public static GlobalFtueSettings FtueSettings => Instance.m_ftueSettings;

        public static void Save()
        {
            var configJson = JsonUtility.ToJson(Instance.m_playerSettings.PlayerConfigs);
            PlayerPrefs.SetString(CONFIG_KEY, configJson);
        }

        public void ToggleHandForceRelease(bool value)
        {
            PlayerSettings.HandsReleaseOnInvalidPosition = value;
        }

        private void OnValidate()
        {
            FtueSettings.OnValidate?.Invoke();
        }

        private const string CONFIG_KEY = "config";

        private void Setup()
        {
            Application.wantsToQuit += () => { Save(); return true; };

            if (PlayerPrefs.HasKey(CONFIG_KEY))
            {
                var configJson = PlayerPrefs.GetString(CONFIG_KEY);
                var config = JsonUtility.FromJson<PlayerConfigs>(configJson);
                PlayerSettings.PlayerConfigs = config;
            }
            else
            {
                PlayerSettings.PlayerConfigs = new PlayerConfigs()
                {
                    Seated = false,
                    Height = 175,
                    SeatedHeight = 92.5f,
                    ComfortLevel = ComfortLevel.Off,
                    ReorientPlayer = 1,
                    MuteMusic = false,
                    MuteVoices = false,
                    DisableCaptions = false,
                };
            }

            PlayerSettings.ComfortLevelChanged();
            PlayerSettings.PlayerCalibrationChanged();
            PlayerSettings.MusicMuted = PlayerSettings.MusicMuted;
            PlayerSettings.VoiceMuted = PlayerSettings.VoiceMuted;
        }

        public enum ComfortLevel
        {
            Off,
            Low,
            Medium,
            High
        }

        [Serializable]
        public struct ComfortPreset
        {
            //public float ReorientPlayerStrength;
            public float BoatMovementStrength;
            public float BoatReactionStrength;
        }

        [Serializable]
        public struct PlayerConfigs
        {
            public bool Seated;
            public float Height;
            public float SeatedHeight;
            public ComfortLevel ComfortLevel;
            public float ReorientPlayer;

            public bool MuteMusic;
            public bool MuteVoices;
            public bool DisableCaptions;
        }

        [ContextMenu("Clear prefs")]
        public void ClearPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}
