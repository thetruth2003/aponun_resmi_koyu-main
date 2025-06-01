using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;


public class AutoPlayTimeline : MonoBehaviour
{
    public PlayableDirector director;

    void Start()
    {
        director.Play();
    }
}
 

