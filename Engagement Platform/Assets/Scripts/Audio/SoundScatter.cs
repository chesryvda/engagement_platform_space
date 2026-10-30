using UnityEngine;

public enum ScatterPlacement
{
    [Tooltip("At a random spot around the player's head (cockpit details, things hitting the hull).")]
    Around,
    [Tooltip("Flies past the player in a straight line, with Doppler (debris, asteroids).")]
    Flyby
}

/// <summary>
/// A group of clips that play now and then at random moments, from random places, with a bit of random pitch
/// and volume. Used for cockpit details, debris ticks, asteroid fly-bys and so on.
/// The owner calls Tick every frame and says whether the sounds should be playing right now.
/// </summary>
[System.Serializable]
public class SoundScatter
{
    [Tooltip("The variants. One is picked at random each time (never the same one twice in a row).")]
    public AudioClip[] clips;
    public AudioCategory category = AudioCategory.Effects;
    public ScatterPlacement placement = ScatterPlacement.Around;

    [Header("When")]
    [Tooltip("Seconds between two sounds (random between these two numbers).")]
    public Vector2 interval = new Vector2(4f, 9f);

    [Header("How loud and how high")]
    [Tooltip("Volume (random between these two numbers).")]
    public Vector2 level = new Vector2(0.7f, 1f);
    [Tooltip("Pitch (random between these two numbers).")]
    public Vector2 pitch = new Vector2(0.9f, 1.1f);

    [Header("Where (in metres, from the player's head)")]
    [Tooltip("Around: how far away. Flyby: how close it passes.")]
    public Vector2 distance = new Vector2(1.5f, 3f);
    [Tooltip("Up and down, relative to the head.")]
    public Vector2 height = new Vector2(-0.3f, 0.8f);
    [Tooltip("Around only: angle from straight ahead. 0 = ahead, 90 = right, 180 = behind.")]
    public Vector2 angle = new Vector2(-180f, 180f);
    [Tooltip("Flyby only: how far ahead of and behind the player it travels.")]
    public float flybyHalfLength = 30f;
    [Tooltip("The furthest the sound can be heard from.")]
    public float hearingRange = 15f;

    private float nextTime = -1f;
    private int lastIndex = -1;

    /// <summary>Call every frame. 'frame' is the ship: positions are measured in its axes.</summary>
    public void Tick(bool active, ExperienceAudio audio, Transform frame)
    {
        if (!active || audio == null || clips == null || clips.Length == 0)
        {
            nextTime = -1f; // when it becomes active again, wait a normal interval first
            return;
        }

        if (nextTime < 0f) nextTime = Time.time + Random.Range(interval.x, interval.y);
        if (Time.time < nextTime) return;
        nextTime = Time.time + Random.Range(interval.x, interval.y);

        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && index == lastIndex) index = (index + 1) % clips.Length;
        lastIndex = index;

        AudioClip clip = clips[index];
        float volume = Random.Range(level.x, level.y);
        float tone = Random.Range(pitch.x, pitch.y);
        float away = Random.Range(distance.x, distance.y);
        float up = Random.Range(height.x, height.y);

        if (placement == ScatterPlacement.Flyby)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            var from = new Vector3(side * away, up, flybyHalfLength);
            var to = new Vector3(side * away, up, -flybyHalfLength);
            audio.Flyby(clip, category, frame, from, to, volume, tone, 2f, hearingRange);
        }
        else
        {
            float yaw = Random.Range(angle.x, angle.y);
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * away + Vector3.up * up;
            audio.PlayAround(clip, category, frame, offset, volume, tone, 1f, hearingRange);
        }
    }
}
