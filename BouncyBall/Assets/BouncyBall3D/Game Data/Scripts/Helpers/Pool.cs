﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool : MonoBehaviour
{
    [SerializeField] int startAmount = 1;
    [SerializeField] GameObject itemPrefab;

    Queue<GameObject> poolQueue = new Queue<GameObject>();

    void Awake()
    {
        if (startAmount > 0)
        {
            for (int i = 0; i < startAmount; i++)
            {
                GameObject newItem = Instantiate(itemPrefab, transform);
                newItem.SetActive(false);
                poolQueue.Enqueue(newItem);
            }
        }
    }

    void AddToPool(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject newItem = Instantiate(itemPrefab, transform);
            newItem.SetActive(false);
            poolQueue.Enqueue(newItem);
        }
    }

    public GameObject GetItem
    {
        get
        {
            if (poolQueue.Count < 1)
                AddToPool(1);

            GameObject item = poolQueue.Dequeue();
            item.SetActive(true);
            return item;
        }
    }

    public void ReturnItem(GameObject item)
    {
        item.name = itemPrefab.name;
        //item.SetActive(false);
        item.GetComponent<Animator>().ResetTrigger("Perfect");
        poolQueue.Enqueue(item);
    }
}
