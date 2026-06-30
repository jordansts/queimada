using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Animator))]
public class ArenaThrowClipPlayer : MonoBehaviour
{
    [SerializeField] private AnimationClip throwClip;
    [SerializeField] [Range(0f, 1f)] private float releaseNormalizedTime = 0.78f;

    public float ReleaseDelay => throwClip != null ? throwClip.length * releaseNormalizedTime : 0f;
    public bool IsThrowing => isThrowing;

    private Animator animator;
    private PlayableGraph throwGraph;
    private bool isThrowing;
    private float throwTimer;

    public void Initialize(Transform actorRoot)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnDisable()
    {
        StopThrowGraph();
    }

    private void OnDestroy()
    {
        StopThrowGraph();
    }

    public void PlayThrow()
    {
        if (throwClip == null || animator == null)
        {
            return;
        }

        StopThrowGraph();

        throwGraph = PlayableGraph.Create($"{name}_ThrowGraph");
        throwGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(throwGraph, "Throw", animator);
        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(throwGraph, throwClip);
        clipPlayable.SetApplyFootIK(false);
        clipPlayable.SetApplyPlayableIK(false);
        output.SetSourcePlayable(clipPlayable);

        throwTimer = 0f;
        isThrowing = true;
        throwGraph.Play();
    }

    private void Update()
    {
        if (!isThrowing || throwClip == null)
        {
            return;
        }

        throwTimer += Time.deltaTime;
        if (throwTimer < throwClip.length)
        {
            return;
        }

        StopThrowGraph();
    }

    private void StopThrowGraph()
    {
        if (throwGraph.IsValid())
        {
            throwGraph.Destroy();
        }

        isThrowing = false;
        throwTimer = 0f;
    }
}
