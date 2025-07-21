using IPGModAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehvLoad : MonoBehaviour
{
    public Func<BehvLoader> loader;
    public BehvLoader loa;

    private void Start()
    {
        loa?.Start();
    }
    private void Update()
    {
        loa?.Update();
    }
    private void OnDestroy()
    {
        loa?.OnDestroy();
    }

    public void Attach(Func<BehvLoader> loader)
    {
        this.loader = loader;
        loa = loader?.Invoke();
        if (loa != null)
        {
            loa._gameObject = () => gameObject;
            loa._transform = () => transform;
        }
    }
}
public class BehvLoader
{
    public virtual void Start() { Debug.LogError("method is null"); }
    public virtual void Update() { }
    public virtual void Awake() { }
    public virtual void OnDestroy() { }

    public Func<GameObject> _gameObject;
    public Func<Transform> _transform;
    public GameObject gameObject => _gameObject?.Invoke();
    public Transform transform => _transform?.Invoke();
}