using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class BackgroundVideoLooper : MonoBehaviour
{
    [Header("Clip")]
    [SerializeField] private VideoClip clip;

    [Header("Loop (frames)")]
    [Tooltip("First frame of the loop (0 = beginning of clip).")]
    [SerializeField] private long startFrame = 0;

    [Tooltip("Last frame of the loop (0 = use last frame of clip).")]
    [SerializeField] private long endFrame = 0;

    [Header("Playback")]
    [SerializeField] private bool playBackward = false;
    [SerializeField] private bool playOnAwake = true;

    private VideoPlayer vp;
    private bool prepared;
    private long effectiveEndFrame;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.skipOnDrop = false;

        if (clip != null)
            vp.clip = clip;

        vp.renderMode = VideoRenderMode.MaterialOverride;
        // If this script is on RoomBackground, target its SpriteRenderer
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            vp.targetMaterialRenderer = sr;
            vp.targetMaterialProperty = "_MainTex";
        }

        vp.prepareCompleted += OnPrepared;
        vp.Prepare();
    }

    private void OnPrepared(VideoPlayer source)
    {
        prepared = true;

        long totalFrames = (long)vp.frameCount;
        effectiveEndFrame = (endFrame > 0 && endFrame < totalFrames)
            ? endFrame
            : totalFrames - 1;

        long start = System.Math.Max(0L, System.Math.Min(startFrame, effectiveEndFrame));

        vp.playbackSpeed = playBackward ? -1f : 1f;
        vp.frame = playBackward ? effectiveEndFrame : start;

        if (playOnAwake)
            vp.Play();
    }

    void Update()
    {
        if (!prepared || vp.clip == null)
            return;

        long f = vp.frame;
        long start = System.Math.Max(0L, System.Math.Min(startFrame, effectiveEndFrame));

        if (!playBackward)
        {
            if (f >= effectiveEndFrame)
                RestartForward(start);
        }
        else
        {
            if (f <= start)
                RestartBackward();
        }
    }

    private void RestartForward(long start)
    {
        vp.Pause();
        vp.frame = start;
        vp.playbackSpeed = 1f;
        vp.Play();
    }

    private void RestartBackward()
    {
        vp.Pause();
        vp.frame = effectiveEndFrame;
        vp.playbackSpeed = -1f;
        vp.Play();
    }
}