﻿using System.Collections;
using RPG.Control;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using GameDevTV.Saving;

namespace RPG.SceneManagement
{
    public class Portal : MonoBehaviour
    {
        enum DestinationIdentifier
        {
            A, B, C, D
        }

        [SerializeField] private float fadeInTime = 1f;
        [SerializeField] private float fadeOutTime = 1f;
        [SerializeField] private float fadeWaitTime = 0.5f;
        [SerializeField] int sceneIndex;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private DestinationIdentifier _destination;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                SceneManager.LoadScene(sceneIndex);
                StartCoroutine(Transition());
            }
            
        }

        private IEnumerator Transition()
        {
            if (sceneIndex < 0)
            {
                Debug.LogError("Scene to load not set.");
                yield break;
            }

            DontDestroyOnLoad(gameObject);

            Fader fader = FindObjectOfType<Fader>();
            SavingWrapper savingWrapper = FindObjectOfType<SavingWrapper>(); 
            PlayerController playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            playerController.enabled = false;

            fader.FadeOutImmediate();
            //yield return fader.FadeOut(fadeOutTime);
            
            savingWrapper.Save();

            yield return SceneManager.LoadSceneAsync(sceneIndex);
            PlayerController newPlayerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            newPlayerController.enabled = false;
            
            savingWrapper.Load();
            
            Portal otherPortal = GetOtherPortal();
            UpdatePlayer(otherPortal);
            
            savingWrapper.Save();

            yield return new WaitForSeconds(fadeWaitTime);
            fader.FadeIn(fadeInTime);

            newPlayerController.enabled = true;
            
            Destroy(gameObject);
        }

        private Portal GetOtherPortal()
        {
            Portal[] portals = FindObjectsOfType<Portal>();
            foreach (Portal portal in portals)
            {
                if (portal == this)
                {
                    continue;
                }

                if (portal._destination == this._destination)
                {
                    return portal;
                }
            }
            return null;
        }

        private void UpdatePlayer(Portal portal)
        {
            GameObject player = GameObject.FindWithTag("Player");
            player.GetComponent<NavMeshAgent>().Warp(portal.spawnPoint.position);
            player.transform.rotation = portal.spawnPoint.rotation;
        }
    }
}

