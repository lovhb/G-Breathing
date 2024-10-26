using System;
using System.Collections;
using System.IO;
using System.Reflection;
using ModLoader.Framework;
using ModLoader.Framework.Attributes;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using static GBreathing.Logger;

namespace GBreathing;

[ItemId("NotPolar.GBreathing")]
public class Main : VtolMod
{
    private static string _modFolder;
    private static AudioSource _audioSource;
    private static AudioSource _endingAudioSource;
    public AudioClip breathingClip;
    public AudioClip endingClip;
    private static FlightInfo _flightInfo;
    private const float VolumeReductionFactor = 0.5f;

    private void Awake()
    {
        _modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (_modFolder == null)
        {
            LogError("Mod folder path is null");
            return;
        }
        Log($"Awake at {_modFolder}");

        // Add and configure AudioSource components
        _audioSource = ConfigureAudioSource(gameObject.AddComponent<AudioSource>(), true);
        _endingAudioSource = ConfigureAudioSource(gameObject.AddComponent<AudioSource>(), false);
        Log("AudioSource components added");

        // Load the audio clips from the "Sounds" subfolder
        StartCoroutine(LoadAudio(Path.Combine(_modFolder, "Sounds", "GBreath.ogg"), clip => breathingClip = clip, _audioSource));
        StartCoroutine(LoadAudio(Path.Combine(_modFolder, "Sounds", "GBreath_End.ogg"), clip => endingClip = clip, _endingAudioSource));
    }
    
    private static AudioSource ConfigureAudioSource(AudioSource source, bool loop)
    {
        source.loop = loop;
        return source;
    }

    public override void UnLoad()
    {
        Log("Unloading mod");
        Destroy(_audioSource);
        Destroy(_endingAudioSource);
        Resources.UnloadAsset(breathingClip);
        Resources.UnloadAsset(endingClip);
    }

    public static void SetFlightInfo(FlightInfo info)
    {
        _flightInfo = info;
        Log("FlightInfo component set");
    }

    public static void SetMixerGroup(AudioMixerGroup group)
    {
        Log("Setting mixer group");
        if (group != null)
        {
            _audioSource.outputAudioMixerGroup = group;
            _endingAudioSource.outputAudioMixerGroup = group;
            Log("Mixer group set to " + group.name);
        }
        else
        {
            LogError("Mixer group is null");
        }
    }

    private static IEnumerator LoadAudio(string path, Action<AudioClip> setClip, AudioSource source)
    {
        Log($"Starting audio load coroutine for path: {path}");
        using var www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.OGGVORBIS);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError ||
            www.result == UnityWebRequest.Result.ProtocolError)
        {
            LogError($"Error loading audio: {www.error}");
        }
        else
        {
            var clip = DownloadHandlerAudioClip.GetContent(www);
            setClip(clip);
            source.clip = clip;
            Log("Audio clip loaded successfully");
        }
    }

    private void Update()
    {
        if (_flightInfo == null) return;

        //Log($"Current player Gs: {flightInfo.playerGs}");
        var volume = CalculateVolume(_flightInfo.playerGs);
        var flybyCamera = FlybyCameraMFDPage.instance;

        if (_flightInfo.playerGs > 5)
            PlayBreathing();
        else
            StopBreathing();

        if (flybyCamera.cameraAudio && !flybyCamera.isInterior)
        {
            _audioSource.volume = 0;
            _endingAudioSource.volume = 0;
        }
        else
        {
            _audioSource.volume = volume * VolumeReductionFactor;
            _endingAudioSource.volume = VolumeReductionFactor;
        }
    }

    private static float CalculateVolume(float playerGs)
    {
        // Assuming Gs range from 0 to 10, map this to volume range 0 to 1 using a quadratic function
        var normalizedGs = Mathf.Clamp(playerGs / 10f, 0f, 1f);
        return normalizedGs * normalizedGs; // Quadratic function for smoother fade-in
    }

    private static void PlayBreathing()
    {
        if (!_audioSource.isPlaying)
        {
            Log("Playing breathing sound");
            _endingAudioSource.Stop();
            _audioSource.Play();
        }
    }

    private static void StopBreathing()
    {
        if (!_audioSource.isPlaying) return;

        //Log("Stopping breathing sound");
        _audioSource.Stop();
        PlayEndingSound();
    }

    private static void PlayEndingSound()
    {
        if (_endingAudioSource.isPlaying) return;
        
        _endingAudioSource.volume = VolumeReductionFactor;
        _endingAudioSource.Play();
    }
}