using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Fusion.UI
{
    public interface IMutableState<T> : IState<T>, IMutableResult<T>
    { }

    public class MutableState<T> : State<T>, IMutableState<T>
    {
        private volatile StandaloneComputed<T> _computed;
        private Result<T> _output;
        protected readonly object Lock = new object();

        public override IComputed<T> Computed => _computed;

        public MutableState(ResultBox<T> output) : this(null, output) { }
        public MutableState(
            IServiceProvider? serviceProvider,
            ResultBox<T> output)
        {
            _output = output;
            _computed = (StandaloneComputed<T>) Fusion.Computed.New(
                serviceProvider, (ComputedUpdater<T>) UpdateAsync, output, true);
        }

        private Task UpdateAsync(IComputed<T> prev, IComputed<T> next, CancellationToken cancellationToken)
        {
            next.TrySetOutput(_output);
            return Task.CompletedTask;
        }

        void IMutableResult.SetValue(object? value) => Update(Result.New((T) value!));
        void IMutableResult.Update(IResult result) => Update(result.AsResult<T>());
        public void SetValue(T value) => Update(Result.New(value));
        public void SetError(Exception error) => Update(Result.Error<T>(error));
        public void Update(Result<T> result)
        {
            lock (Lock) {
                if (_computed.State != ComputedState.Invalidated) {
                    _computed.Invalidate();
                    OnInvalidated();
                }
                _output = result;
                var task = _computed.UpdateAsync(false, CancellationToken.None);
                if (!task.IsCompleted)
                    throw Errors.InternalError($"{nameof(IComputed.UpdateAsync)} must complete synchronously here.");
                _computed = (StandaloneComputed<T>) task.Result;
            }
            OnUpdated();
        }
    }
}
