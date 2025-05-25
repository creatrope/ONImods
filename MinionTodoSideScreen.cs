// Add this field to your MinionTodoSideScreen class
public LocText artifactTestLabel;

// In OnShow or SetTarget, set the test label text
protected override void OnShow(bool show)
{
    base.OnShow(show);
    this.refreshHandle.ClearScheduler();
    if (!show)
    {
        if (!this.useOffscreenIndicators)
            return;
        foreach (GameObject choreTarget in this.choreTargets)
            OffscreenIndicator.Instance.DeactivateIndicator(choreTarget);
    }
    else
    {
        if ((UnityEngine.Object) DetailsScreen.Instance.target == (UnityEngine.Object) null)
            return;
        this.choreConsumer = DetailsScreen.Instance.target.GetComponent<ChoreConsumer>();
        this.PopulateElements();

        // Set test label text
        if (artifactTestLabel != null)
            artifactTestLabel.text = "Artifact Effects: [TEST LABEL]";
    }
}