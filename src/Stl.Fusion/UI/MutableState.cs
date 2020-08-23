using System;
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
        public new T Value {
            get => base.Value;
            set => Update(Result.Value(value));
        }
        public new Exception? Error {
            get => base.Error;
            set => Update(Result.Error<T>(value));
        }
        object? IMutableResult.UntypedValue {
            get => base.Value;
            set => Update(Result.Value((T) value!));
        }

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

        void IMutableResult.Update(IResult result)
            => Update(result.AsResult<T>());
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
