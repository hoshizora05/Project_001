using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SceneManagement;
using System.Threading.Tasks;

public class GameScene : Scene
{
    protected override void OnInitialize()
    {
    }

    protected override async Task OnShow()
    {
        await base.OnShow();
    }

    protected override async Task OnHide()
    {
        await base.OnHide();
    }

    protected override async Task OnFinalize()
    {
        await base.OnFinalize();
    }
}
