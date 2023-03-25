using UnityEngine;

namespace SimplestarGame
{
    public class Bullet : MonoBehaviour
    {
        internal System.Action<Vector3> onFragment;

        void OnCollisionEnter(Collision collision)
        {
            if (this.flagmented)
            {
                return;
            }
            if (collision.gameObject.TryGetComponent(out VoronoiFragmenter voronoiFragmenter))
            {
                foreach (var contact in collision.contacts)
                {
                    voronoiFragmenter.Fragment(new RaycastHit { point = contact.point, normal = contact.normal });
                    this.onFragment?.Invoke(contact.point);
                    this.flagmented = true;
                    break;
                }
            }
        }

        bool flagmented = false;
    }
}