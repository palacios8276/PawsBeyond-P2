using UnityEngine;
using Unity.Cinemachine;

// Keeps the camera's positional target independent from the dog's yaw.
[DefaultExecutionOrder(-50)]
public class CameraTargetFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private CinemachinePanTilt orbit;

    private void LateUpdate()
    {
        if (player == null) return;
        transform.position = player.position + Vector3.up * heightOffset;
        // The mouse/stick orbit drives the rig; the dog's yaw never drives it.
        transform.rotation = orbit
            ? Quaternion.Euler(orbit.TiltAxis.Value, orbit.PanAxis.Value, 0f)
            : Quaternion.identity;
    }
}

