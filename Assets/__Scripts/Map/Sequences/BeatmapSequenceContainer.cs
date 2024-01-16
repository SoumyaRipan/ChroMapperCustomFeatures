using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapSequenceContainer : BeatmapObjectContainer
{
    private static readonly int colorTint = Shader.PropertyToID("_ColorTint");
    private static readonly int shaderScale = Shader.PropertyToID("_WorldScale");

    [SerializeField] private TracksManagerRight manager;

    public BeatmapSequence SequenceData;

    public override BeatmapObject ObjectData { get => SequenceData; set => SequenceData = (BeatmapSequence)value; }

    public int ChunkEnd => (int)((SequenceData.Time + SequenceData.Duration) / Intersections.ChunkSize);

    public bool IsRotatedByNoodleExtensions =>
        SequenceData.CustomData != null && (SequenceData.CustomData?.HasKey("_rotation") ?? false);

    public static BeatmapSequenceContainer SpawnSequence(BeatmapSequence data, TracksManagerRight manager, ref GameObject prefab)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapSequenceContainer>();
        container.SequenceData = data;
        container.manager = manager;
        return container;
    }

    public void SetColor(Color color)
    {
        MaterialPropertyBlock.SetColor(colorTint, color);
        UpdateMaterials();
    }

    public void SetScale(Vector3 scale)
    {
        transform.localScale = scale;

        MaterialPropertyBlock.SetVector(shaderScale, scale);
        UpdateMaterials();
    }

    public override void UpdateGridPosition()
    {
        var duration = SequenceData.Duration;
        var localRotation = Vector3.zero;

        //Take half jump duration into account if the setting is enabled.
        if (SequenceData.Duration < 0 && Settings.Instance.ShowMoreAccurateFastWalls)
        {
            var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var songNoteJumpSpeed = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
            var songStartBeatOffset = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;

            var halfJumpDuration = SpawnParameterHelper.CalculateHalfJumpDuration(songNoteJumpSpeed, songStartBeatOffset, bpm);

            duration -= duration * Mathf.Abs(duration / halfJumpDuration);
        }

        duration *= EditorScaleController
            .EditorScale; // Apply Editor Scale here since it can be overwritten by NE _scale Z

        if (SequenceData.CustomData != null)
        {
            if (SequenceData.CustomData.HasKey("_scale"))
            {
                if (SequenceData.CustomData["_scale"].Count > 2) //Apparently scale supports Z now, ok
                    duration = SequenceData.CustomData["_scale"]?.ReadVector3().z ?? duration;
            }

            if (SequenceData.CustomData.HasKey("_localRotation"))
                localRotation = SequenceData.CustomData["_localRotation"]?.ReadVector3() ?? Vector3.zero;
            if (SequenceData.CustomData.HasKey("_rotation"))
            {
                Track track = null;
                if (SequenceData.CustomData["_rotation"].IsNumber)
                {
                    float rotation = SequenceData.CustomData["_rotation"];
                    track = manager.CreateTrack(rotation);
                }
                else if (SequenceData.CustomData["_rotation"].IsArray)
                {
                    track = manager.CreateTrack(SequenceData.CustomData["_rotation"].ReadVector3());
                }

                if (track != null)
                {
                    track.AttachContainer(this);
                }
            }
        }

        var bounds = SequenceData.GetShape();

        // Enforce positive scale, offset our sequences to match.
        transform.localPosition = new Vector3(
            bounds.Position + (bounds.Width < 0 ? bounds.Width : 0),
            bounds.StartHeight + (bounds.Height < 0 ? bounds.Height : 0),
            (SequenceData.Time * EditorScaleController.EditorScale) + (duration < 0 ? duration : 0)
        );

        SetScale(new Vector3(
            Mathf.Abs(bounds.Width),
            Mathf.Abs(bounds.Height),
            Mathf.Abs(duration)
        ));

        if (localRotation != Vector3.zero)
        {
            transform.localEulerAngles = Vector3.zero;
            var side = transform.right.normalized * (bounds.Width / 2);
            var rectWorldPos = transform.position + side;

            transform.RotateAround(rectWorldPos, transform.right, localRotation.x);
            transform.RotateAround(rectWorldPos, transform.up, localRotation.y);
            transform.RotateAround(rectWorldPos, transform.forward, localRotation.z);
        }

        UpdateCollisionGroups();
    }
}