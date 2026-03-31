using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class ReadyButton : MonoBehaviour
    {
        private Button button;
        // public event Action ready;
        //  public static ReadyButton current;
        public bool ready = false;
        [SerializeField] private Button one;
        [SerializeField] private Button two;
        [SerializeField] private Button three;
        [SerializeField] private Button four;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            button = GetComponent<Button>();
            button.interactable = true;
            // current = this;
        }

        // Update is called once per frame
        private void Update()
        {
            // TODO - readd this, gives error rn
            //might be better to create an event trigger but this is functional
            // if (button.interactable == false & (one.interactable == false & two.interactable == false & three.interactable == false & four.interactable == false))
            // {
            //     ReadyTrigger();
            // }
            /*else if (button.interactable != false)
        {
            TakeBack();
        }*/
        }

        public void Ready()
        {
            SceneManager.LoadScene("GamePlay");
        }

        public void ReadyTrigger()
        {
            button.interactable = true;
        }

        public void TakeBack()
        {
            button.interactable = false;
        }

    }
}
