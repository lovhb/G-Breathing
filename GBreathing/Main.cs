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
    public static string ModFolder;
    private static AudioSource audioSource;
    private static AudioSource endingAudioSource;
    public AudioClip breathingClip;
    public AudioClip endingClip;
    private static FlightInfo flightInfo;
    private const float VolumeReductionFactor = 0.5f;
    public AudioMixerGroup interiorMixerGroup;

    private void Awake()
    {
        ModFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Log($"Awake at {ModFolder}");

        // Add AudioSource components
        audioSource = gameObject.AddComponent<AudioSource>();
        endingAudioSource = gameObject.AddComponent<AudioSource>();
        Log("AudioSource components added");

        // Set the loop property to true
        audioSource.loop = true;

        // Load the audio clips
        var audioPath = Path.Combine(ModFolder, "GBreath.ogg");
        var endingAudioPath = Path.Combine(ModFolder, "GBreath_End.ogg");
        Log($"Loading audio from path: {audioPath}");
        StartCoroutine(LoadAudio(audioPath, clip => breathingClip = clip, audioSource));
        Log($"Loading ending audio from path: {endingAudioPath}");
        StartCoroutine(LoadAudio(endingAudioPath, clip => endingClip = clip, endingAudioSource));
    }

    public override void UnLoad()
    {
        Log("Unloading mod");
        // Destroy any objects
        Destroy(audioSource);
        Destroy(endingAudioSource);

        // Unload any resources
        Resources.UnloadAsset(breathingClip);
        Resources.UnloadAsset(endingClip);
    }

    public static void SetFlightInfo(FlightInfo info)
    {
        flightInfo = info;
        Log("FlightInfo component set");
    }

    public static void SetMixerGroup(AudioMixerGroup group)
    {
        Log("Setting mixer group");
        if (group != null)
        {
            // Set the mixer group
            audioSource.outputAudioMixerGroup = group;
            endingAudioSource.outputAudioMixerGroup = group;
            Log("Mixer group set to" + group.name);
        }
        else
        {
            LogError("Mixer group is null");
        }
    }

    private IEnumerator LoadAudio(string path, Action<AudioClip> setClip, AudioSource source)
    {
        Log($"Starting audio load coroutine for path: {path}");
        using (var www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.OGGVORBIS))
        {
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
    }

    private void Update()
    {
        if (flightInfo != null)
        {
            //Log($"Current player Gs: {flightInfo.playerGs}");
            var volume = CalculateVolume(flightInfo.playerGs);

            if (flightInfo.playerGs > 6)
                PlayBreathing();
            else
                StopBreathing();

            if (FlybyCameraMFDPage.instance.cameraAudio && !FlybyCameraMFDPage.instance.isInterior)
            {
                audioSource.volume = 0;
                endingAudioSource.volume = 0;
                //Log("Muting audio because player is in exterior view");
            }
            else
            {
                audioSource.volume = volume * VolumeReductionFactor;
                endingAudioSource.volume = VolumeReductionFactor;
                //Log("Setting audio volume to " + volume);
            }
        }
    }

    private float CalculateVolume(float playerGs)
    {
        // Assuming Gs range from 0 to 10, map this to volume range 0 to 1 using a quadratic function
        var normalizedGs = Mathf.Clamp(playerGs / 10f, 0f, 1f);
        return normalizedGs * normalizedGs; // Quadratic function for smoother fade-in
    }

    private void PlayBreathing()
    {
        if (!audioSource.isPlaying)
        {
            Log("Playing breathing sound");
            audioSource.Play();
        }
        else
        {
            Log("Breathing sound is already playing");
        }
    }

    private void StopBreathing()
    {
        if (audioSource.isPlaying)
        {
            Log("Stopping breathing sound");
            audioSource.Stop();
            PlayEndingSound();
        }
    }

    private void PlayEndingSound()
    {
        if (!endingAudioSource.isPlaying)
        {
            Log("Playing ending sound");
            endingAudioSource.volume = VolumeReductionFactor; // Apply volume reduction
            endingAudioSource.Play();
        }
        else
        {
            Log("Ending sound is already playing");
        }
    }
}