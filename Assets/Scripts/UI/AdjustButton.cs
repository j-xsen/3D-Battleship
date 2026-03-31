using TMPro;
using UnityEngine;

namespace UI
{
    public class AdjustButton : MonoBehaviour
    {
        private TMP_Text child;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            child = GetComponentInChildren<TMP_Text>();
            child.text = "Adjust";
        }

        // Update is called once per frame
        public void Clicked()
        {
            if (child.text == "Adjust")
            {
                child.text = "Place";
            }
            else
            {
                child.text = "Adjust";
            }
        }
    }
}
