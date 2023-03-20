using UnityEngine;

namespace SimplestarGame
{
    public class MeshCleaner : MonoBehaviour
    {
        void OnDestroy()
        {
            if (this.TryGetComponent(out MeshCollider meshCollider))
            {
                meshCollider.sharedMesh = null;
            }
            if (this.TryGetComponent(out MeshFilter meshFilter))
            {
                var mesh = meshFilter.sharedMesh;
                meshFilter.sharedMesh = null;
                mesh.Clear();
                GameObject.Destroy(mesh);
            }
        }
    }
}