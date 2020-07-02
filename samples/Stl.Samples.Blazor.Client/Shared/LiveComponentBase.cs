using System;
using Microsoft.AspNetCore.Components;
using Stl.Fusion.UI;

namespace Stl.Samples.Blazor.Client.Shared
{
    public abstract class LiveComponentBase<TModel> : ComponentBase, IDisposable
    {
        [Inject]
        protected ILive<TModel> Live { get; set; } = null!;
        protected TModel Model => Live.Value;
        protected IUpdateDelayer UpdateDelayer => Live.UpdateDelayer;

        public virtual void Dispose() 
            => Live.Dispose();

        public void UpdateModel(bool immediately = true)
        {
            Live.Invalidate();
            if (immediately)
                UpdateDelayer.CancelDelays();
        }

        protected override void OnInitialized() 
            => Live.Updated += OnModelUpdated;

        protected virtual void OnModelUpdated(ILive live) 
            => StateHasChanged();
    }
}
