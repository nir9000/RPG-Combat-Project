﻿using System;
using RPG.Control;
using RPG.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace RPG.Cinematics
{
    public class CinematicControlRemover : MonoBehaviour
    {
        private GameObject player;

        private void Awake()
        {
            player = GameObject.FindWithTag("Player");
        }

        private void OnEnable()
        {
            GetComponent<PlayableDirector>().stopped += EnableControl;
            GetComponent<PlayableDirector>().played += DisableControl;
        }

        private void OnDisable()
        {
            GetComponent<PlayableDirector>().stopped -= EnableControl;
            GetComponent<PlayableDirector>().played -= DisableControl;
        }


        void DisableControl(PlayableDirector director)
        {
            player.GetComponent<ActionScheduler>().CancelCurrentAction();
            player.GetComponent<PlayerController>().enabled = false;
        }

        void EnableControl(PlayableDirector director)
        {
            player.GetComponent<PlayerController>().enabled = true;
        }
    }
}

