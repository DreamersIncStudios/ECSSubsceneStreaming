using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    public class BuildConfirmation : MonoBehaviour
    {
        public bool IsValid => transform.gameObject.activeSelf;
        public float3 Position => transform.position;

        public void Show(float3 position)
        {
            transform.gameObject.SetActive(true);
            transform.position = position;
        }

        public void Hide()
        {
            transform.gameObject.SetActive(false);
        }

        void Awake()
        {
            Hide();
        }
    }
}
