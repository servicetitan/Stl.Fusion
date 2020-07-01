using System;
using Microsoft.AspNetCore.Components;
using Stl.Fusion.UI;

namespace Stl.Samples.Blazor.Client.Shared
{
    public abstract class LiveComponentBase<TModel> : ComponentBase, IDisposable
    {
        [Inject]
        protected ILive<TModel> LiveModel { get; set; } = null!;
        protected TModel Model => LiveModel.Value;
        protected IUpdateDelayer UpdateDelayer => LiveModel.UpdateDelayer;

        protected override void OnInitialized() 
            => LiveModel.Updated += OnLiveModelUpdated;

        protected virtual void OnLiveModelUpdated(ILive liveModel) 
            => StateHasChanged();

        public virtual void Dispose() 
            => LiveModel.Dispose();
    }
}
