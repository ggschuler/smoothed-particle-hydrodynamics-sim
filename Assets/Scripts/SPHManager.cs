using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPHManager : MonoBehaviour
{
    [SerializeField] private Transform[] cases;
    private Transform active = null;

    public void ChooseCase(int which)
    {
        switch (which)
        {
            case 0: 
                ActivateCase(cases[0]);
                break;
            case 1:
                ActivateCase(cases[1]);
                break;
            case 2: 
                ActivateCase(cases[2]);
                break;
        }
    }

    private void ActivateCase(Transform t)
    {
        active = t;
        foreach (Transform sph in t)
        {
            sph.gameObject.SetActive(true);
        }
    }

    public void PauseCase()
    {
        foreach (Transform s in active)
        {
            var script = s.gameObject.GetComponent<SPHMonobehavior>();
            script.Pause();
        }
    }

    public void Reset()
    {
        foreach (var c in cases)
        {
            foreach (Transform t in c)
            {
                t.gameObject.SetActive(false);
            }
        }
    }
}
