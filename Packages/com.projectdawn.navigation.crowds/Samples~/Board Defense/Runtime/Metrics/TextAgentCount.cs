using UnityEngine;
using UnityEngine.UI;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    [RequireComponent(typeof(Text))]
    public class TextAgentCount : MonoBehaviour
    {
        Text m_Text;

        void Awake()
        {
            m_Text = GetComponent<Text>();
        }

        public void UpdateCount(int count)
        {
            if (m_Text == null)
                return;
            m_Text.text = $"{count}";
        }
    }
}
