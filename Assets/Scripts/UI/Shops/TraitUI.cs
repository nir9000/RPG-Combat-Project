﻿using System;
using RPG.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.UI
{
    public class TraitUI : MonoBehaviour
    {
    
        [SerializeField] private TextMeshProUGUI unassignedPointsText;
        [SerializeField] private Button commitButton;

        private TraitStore playerTraitStore = null;

        private void Start()
        {
            playerTraitStore = GameObject.FindWithTag("Player").GetComponent<TraitStore>();
            commitButton.onClick.AddListener(playerTraitStore.Commit);
        }

        private void Update()
        {
            unassignedPointsText.text = playerTraitStore.GetUnassignedPoints().ToString();
        }
    }
}