using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VisionConeRenderer : MonoBehaviour
{
    [SerializeField] private EnemyVision vision;
    [SerializeField] private int segments = 36;
    [SerializeField] private float yOffset = 0.04f;

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        mesh = new Mesh { name = "Runtime Vision Cone" };
        GetComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void LateUpdate()
    {
        if (vision == null)
        {
            vision = GetComponentInParent<EnemyVision>();
        }

        if (vision == null)
        {
            return;
        }

        transform.SetPositionAndRotation(vision.transform.position + Vector3.up * yOffset, Quaternion.identity);
        RebuildMesh();

        if (meshRenderer != null)
        {
            var color = vision.CanSeePlayer
                ? new Color(1f, 0.16f, 0.11f, 0.34f)
                : new Color(1f, 0.86f, 0.16f, 0.24f);
            var material = meshRenderer.material;
            material.color = color;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
        }
    }

    public void Configure(EnemyVision enemyVision)
    {
        vision = enemyVision;
    }

    private void RebuildMesh()
    {
        var vertexCount = segments + 2;
        var vertices = new Vector3[vertexCount];
        var triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        for (var i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;
            var angle = -vision.ViewAngle * 0.5f + vision.ViewAngle * t;
            var direction = Quaternion.Euler(0f, angle, 0f) * vision.transform.forward;
            vertices[i + 1] = direction * vision.ViewRadius;

            if (i == segments)
            {
                continue;
            }

            var tri = i * 3;
            triangles[tri] = 0;
            triangles[tri + 1] = i + 1;
            triangles[tri + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}
