using UnityEngine;

public enum PlanetoidFaceType
{
   UP_FACE, DOWN_FACE, LEFT_FACE, RIGHT_FACE, BACK_FACE, FORWARD_FACE
}

public struct PlanetoidFace
{
    public readonly PlanetoidFaceType Type;
    public readonly Vector3 DirVec;
    public readonly Vector3 DirVecAxisA;
    public readonly Vector3 DirVecAxisB;

    public static PlanetoidFace[] FACES = {UP, DOWN, LEFT, RIGHT, FORWARD, BACKWARD};
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

        if (type== PlanetoidFaceType.UP_FACE) DirVec = Vector3.up;
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

        throw new System.Exception(string.Format("{0} is in invalid direction and cannot be mapped to a PlanetoidFace",Direction.ToString()));
    }

    public static Vector3 FaceToDirection(PlanetoidFace Face)
    {
        return Face.DirVec;
    }

    public static Vector3 OnSphereNormalized(Vector3 OnCube)
    {
        Vector3 v = OnCube;
        float x2 = v.x * v.x;
        float y2 = v.y * v.y;
        float z2 = v.z * v.z;

        Vector3 s;
        s.x = v.x * Mathf.Sqrt(Mathf.Abs(1f - (y2 / 2f) - (z2 / 2f) + (y2 * z2 / 3f)));
        s.y = v.y * Mathf.Sqrt(Mathf.Abs(1f - (x2 / 2f) - (z2 / 2f) + (x2 * z2 / 3f)));
        s.z = v.z * Mathf.Sqrt(Mathf.Abs(1f - (x2 / 2f) - (y2 / 2f) + (x2 * y2 / 3f)));

        return s;
    }

    public static Vector3 OnCube(PlanetoidFace Face, float X, float Y, float Edges)
    {
        return OnCube(Face.DirVec, Face.DirVecAxisA, Face.DirVecAxisB, X, Y, Edges);
    }

    public static Vector3 OnCube(Vector3 FaceDir, Vector3 AxisA, Vector3 AxisB, float X, float Y, float Edges)
    {
        Vector2 Percent = new Vector2(X, Y) / (Edges);
        Vector3 OnCube = FaceDir + ((Percent.x - 0.5f) * 2 * AxisA) + ((Percent.y - 0.5f) * 2 * AxisB);
        return OnCube;
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

    public static Vector3 CubifyFromSphereNormalized(Vector3 OnSphere)
    {
        Vector3 s = OnSphere;

        float x2 = Mathf.Sqrt(Mathf.Abs(s.x));
        float y2 = Mathf.Sqrt(Mathf.Abs(s.y));
        float z2 = Mathf.Sqrt(Mathf.Abs(s.z));

        Vector3 v;
        v.x = s.x / (Mathf.Sqrt(Mathf.Abs(1f - (y2 / 2f) - (z2 / 2f) + (y2 * z2 / 3f))));
        v.y = s.y / (Mathf.Sqrt(Mathf.Abs(1f - (x2 / 2f) - (z2 / 2f) + (x2 * z2 / 3f))));
        v.z = s.z / (Mathf.Sqrt(Mathf.Abs(1f - (x2 / 2f) - (y2 / 2f) + (x2 * y2 / 3f))));

        return v;
    }

    public static Vector2 SphereToLocalGrid(Vector3 WorldSpacePlayerLocation, Vector3 WorldSpacePlanetLocation)
    {
        Vector3 Direction = (WorldSpacePlayerLocation - WorldSpacePlanetLocation).normalized;
        return SphereToLocalGrid(FindFace(WorldSpacePlayerLocation, WorldSpacePlanetLocation), Direction);
    }

    public static Vector2 SphereToLocalGrid(PlanetoidFace Face, Vector3 Direction)
    {
        Vector3 ToCube = (CubifyFromSphereNormalized(Direction) + Vector3.one) / 2f;
        Vector2 ToLocalCenterTransformed;

        switch (Face.Type)
        {
            case PlanetoidFaceType.UP_FACE:
                ToLocalCenterTransformed = new Vector2(ToCube.x, 1f - ToCube.z);
                break;
            case PlanetoidFaceType.DOWN_FACE:
                ToLocalCenterTransformed = new Vector2(1f - ToCube.x, 1f - ToCube.z);
                break;
            case PlanetoidFaceType.RIGHT_FACE:
                ToLocalCenterTransformed = new Vector2(ToCube.z, 1f - ToCube.y);
                break;
            case PlanetoidFaceType.LEFT_FACE:
                ToLocalCenterTransformed = new Vector2(1f - ToCube.z, 1f - ToCube.y);
                break;
            case PlanetoidFaceType.FORWARD_FACE:
                ToLocalCenterTransformed = new Vector2(ToCube.y, 1f - ToCube.x);
                break;
            case PlanetoidFaceType.BACK_FACE:
                ToLocalCenterTransformed = new Vector2(1f - ToCube.y, 1f - ToCube.x);
                break;
            default:
                throw new System.Exception("Invalid PlanetoidFaceType provided. Value: " + Face.Type.ToString());
        }

        return ToLocalCenterTransformed;
    }
}