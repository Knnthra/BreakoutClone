using UnityEngine;

public class MarioMode : MonoBehaviour
{

    [field: SerializeField] public WireframeBuildUp WireFrameBuildUp { get; private set; }

    [field: SerializeField] public Mario Mario { get; private set; }

    [field: SerializeField] public PipeEntrance PipeEntrance { get; private set; }

    [field: SerializeField] public QuestionBlock QuestionBlock { get; private set; }

    public void DisableMario()
    {
        if (Mario != null)
        {
            Mario.enabled = false;
            Mario.gameObject.SetActive(false);
        }
    }

    public void EnableMario()
    {
        if (Mario != null)
        {
            Mario.enabled = true;
        }
    }

    public void ShowMario()
    {
        if (Mario != null)
            Mario.gameObject.SetActive(true);
    }

}
