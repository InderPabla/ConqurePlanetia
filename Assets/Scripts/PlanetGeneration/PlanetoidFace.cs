using UnityEngine;

public enum PlanetoidFaceType
{
   NO_FACE, UP_FACE, DOWN_FACE, LEFT_FACE, RIGHT_FACE, BACK_FACE, FORWARD_FACE
}

public struct PlanetoidFace
{
    public readonly PlanetoidFaceType Type;
    public readonly Vector3 DirVec;
    public readonly Vector3 DirVecAxisA;
    public readonly Vector3 DirVecAxisB;

    public static PlanetoidFace[] FACES = { UP, DOWN, LEFT, RIGHT, FORWARD, BACKWARD };
    public static Vector3[] FACE_DIRECTION_VECTORS = { Vector3.left, Vector3.right, Vector3.forward, Vector3.back, Vector3.up, Vector3.down };

    public static PlanetoidFace UP { get { return new PlanetoidFace(PlanetoidFaceType.UP_FACE); } }
    public static PlanetoidFace DOWN { get { return new PlanetoidFace(PlanetoidFaceType.DOWN_FACE); } }
    public static PlanetoidFace LEFT { get { return new PlanetoidFace(PlanetoidFaceType.LEFT_FACE); } }
    public static PlanetoidFace RIGHT { get { return new PlanetoidFace(PlanetoidFaceType.RIGHT_FACE); } }
    public static PlanetoidFace FORWARD { get { return new PlanetoidFace(PlanetoidFaceType.FORWARD_FACE); } }
    public static PlanetoidFace BACKWARD { get { return new PlanetoidFace(PlanetoidFaceType.BACK_FACE); } }

    private PlanetoidFace(PlanetoidFaceType type)
    {
        Type = type;
        DirVec = Vector3.zero;

        if (type == PlanetoidFaceType.UP_FACE) DirVec = Vector3.up;
        else if (type == PlanetoidFaceType.DOWN_FACE) DirVec = Vector3.down;
        else if (type == PlanetoidFaceType.LEFT_FACE) DirVec = Vector3.left;
        else if (type == PlanetoidFaceType.RIGHT_FACE) DirVec = Vector3.right;
        else if (type == PlanetoidFaceType.FORWARD_FACE) DirVec = Vector3.forward;
        else if (type == PlanetoidFaceType.BACK_FACE) DirVec = Vector3.back;

        DirVecAxisA = new Vector3(DirVec.y, DirVec.z, DirVec.x);
        DirVecAxisB = Vector3.Cross(DirVec, DirVecAxisA);
    }

    public static PlanetoidFace DirectionToFace(Vector3 Direction)
    {
        if (Direction == Vector3.up) return UP;
        if (Direction == Vector3.down) return DOWN;
        if (Direction == Vector3.left) return LEFT;
        if (Direction == Vector3.right) return RIGHT;
        if (Direction == Vector3.forward) return FORWARD;
        if (Direction == Vector3.back) return BACKWARD;

        throw new System.Exception(string.Format("{0} is in invalid direction and cannot be mapped to a PlanetoidFace", Direction.ToString()));
    }

    public static Vector3 FaceToDirection(PlanetoidFace Face)
    {
        return Face.DirVec;
    }

    //https://answers.unity.com/questions/617076/calculate-the-general-direction-of-a-vector.html
    public static PlanetoidFace FindFace(Vector3 WorldSpacePlayerLocation, Vector3 WorldSpacePlanetLocation)
    {
        Vector3 CheckVectorDirection = WorldSpacePlayerLocation - WorldSpacePlanetLocation;

        float maxDot = -Mathf.Infinity;
        Vector3 generalFaceDirection = Vector3.zero;

        foreach (Vector3 faceDirection in FACE_DIRECTION_VECTORS)
        {
            float t = Vector3.Dot(CheckVectorDirection, faceDirection);
            if (t > maxDot)
            {
                generalFaceDirection = faceDirection;
                maxDot = t;
            }
        }

        return DirectionToFace(generalFaceDirection);
    }

    public static bool operator == (PlanetoidFace Face1, PlanetoidFace Face2) {
        return Face1.Type == Face2.Type;
    }

    public static bool operator != (PlanetoidFace Face1, PlanetoidFace Face2)
    {
        return Face1.Type != Face2.Type;
    }
}