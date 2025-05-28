using UnityEngine;

public class InverseRotation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Transform beam = GameObject.Find("X axis _assembled - Ender3S1_assembled.STEP-1 Ender3S1_X_Z.042").transform;
        Transform headLocalFix = GameObject.Find("HeadLocalFix").transform;

        // Get beam's rotation in world space
        Quaternion beamWorldRot = beam.rotation;

        // Cancel only Y and Z rotation (keep X axis)
        Vector3 beamEuler = beamWorldRot.eulerAngles;
        Quaternion cancelYZ = Quaternion.Euler(0, -beamEuler.y, -beamEuler.z);

        // Apply to headLocalFix in world space to cancel parent's influence
        headLocalFix.rotation = cancelYZ;
    }

}
