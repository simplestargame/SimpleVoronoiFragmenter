using UnityEngine;

namespace SimplestarGame
{
    public class MonkeyGenerator : MonoBehaviour
    {
        [SerializeField] GameObject monkeyPrefab;
        [SerializeField] Transform[] generatePoints;
        [SerializeField] float interval = 3f;

        void Update()
        {
            if (this.lastTime + this.interval < Time.time)
            {
                this.lastTime = Time.time;

                foreach (var genPoint in this.generatePoints)
                {
                    Collider[] colliders = Physics.OverlapSphere(genPoint.position, 1f);
                    if (0 == colliders.Length)
                    {
                        var monkey = Instantiate(this.monkeyPrefab, genPoint.position, genPoint.rotation, null);
                    }
                }
            }
        }

        float lastTime;
    }
}