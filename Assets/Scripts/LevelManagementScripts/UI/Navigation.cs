﻿using System;
using System.Collections.Generic;
using System.Linq;
using Interact;
using Manager;
using UnityEngine;

public class Navigation : MonoBehaviour
{
    private GameObject _player;
    private List<NavigationElement> _elements;
    public static Navigation Manager;
    [SerializeField]
    GameObject koalaSpritePrefab;
    [SerializeField]
    GameObject fireSpritePrefab;
    [SerializeField]
    GameObject truckSpritePrefab;

    [SerializeField] private Transform _holder;
    private void Awake()
    {
        if (Manager)
        {
            Destroy(gameObject);
        }
        else
        {
            Manager = this;
        }
        _elements = new List<NavigationElement>();
        _player = GameObject.FindGameObjectWithTag("Player");
        EventManager.Instance.OnProblemSolvedEvent += RemoveElement;
        EventManager.Instance.OnGrabableDestroyedEvent += RemoveElement;
    }

    private void OnDestroy()
    {
        EventManager.Instance.OnProblemSolvedEvent -= RemoveElement;
        EventManager.Instance.OnGrabableDestroyedEvent -= RemoveElement;
    }

    public void RemoveElement(Grabable grabable)
    {
        NavigationElement delete = null;
        foreach (var element in _elements)
        {
            if (element.ParentIsNull || !element.Type)
            {
                continue;
            }
            if (element.Type.Equals(grabable))
            {
                delete = element;
            }
        }

        if (delete)
        {
            _elements.Remove(delete);
            delete.gameObject.SetActive(false);
        }
    }

    public void AddElement(GameObject parent, Problem problem)
    {
        GameObject prefab;
        switch (problem)
        {
            case Problem.Animal:
                prefab = koalaSpritePrefab;
                break;
            case Problem.Fire:
                prefab = fireSpritePrefab;
                break;
            case Problem.Truck:
                prefab = truckSpritePrefab;
                break;
            default:
                prefab = null;
                break;
        }
        var navigationElement = Instantiate(prefab, _holder).GetComponent<NavigationElement>();
        navigationElement.FillNavigationElement(parent, _player);
        _elements.Add(navigationElement);
    }
}
