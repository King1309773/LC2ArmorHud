using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace LC2ArmorHud;

public sealed class PluginBehaviour : MonoBehaviour
{
    private Action _tick;
    private Action _reset;
    private UnityAction<Scene, LoadSceneMode> _sceneLoadedHandler;
    private bool _subscribed;

    public PluginBehaviour(IntPtr pointer)
        : base(pointer)
    {
    }

    internal void Initialize(Action tick, Action reset)
    {
        _tick = tick ?? throw new ArgumentNullException(nameof(tick));
        _reset = reset ?? throw new ArgumentNullException(nameof(reset));
        if (_subscribed)
        {
            return;
        }

        _sceneLoadedHandler = new Action<Scene, LoadSceneMode>((_, _) => _reset?.Invoke());
        SceneManager.sceneLoaded += _sceneLoadedHandler;
        _subscribed = true;
    }

    internal void Shutdown()
    {
        if (_subscribed && _sceneLoadedHandler is not null)
        {
            SceneManager.sceneLoaded -= _sceneLoadedHandler;
        }

        _subscribed = false;
        _sceneLoadedHandler = null;
        _tick = null;
        _reset = null;
    }

    private void Update() => _tick?.Invoke();

    private void OnDestroy() => Shutdown();
}
