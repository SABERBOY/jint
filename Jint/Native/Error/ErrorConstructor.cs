using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;

namespace Jint.Native.Error
{
    public sealed class ErrorConstructor : FunctionInstance, IConstructor
    {
        private readonly Func<Intrinsics, ObjectInstance> _intrinsicDefaultProto;

        internal ErrorConstructor(
            Engine engine,
            Realm realm,
            ObjectInstance functionPrototype,
            ObjectInstance objectPrototype,
            JsString name, Func<Intrinsics, ObjectInstance> intrinsicDefaultProto)
            : base(engine, realm, name)
        {
            _intrinsicDefaultProto = intrinsicDefaultProto;
            _prototype = functionPrototype;
            PrototypeObject = new ErrorPrototype(engine, realm, this, objectPrototype, name);
            _length = new PropertyDescriptor(JsNumber.PositiveOne, PropertyFlag.Configurable);
            _prototypeDescriptor = new PropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
        }

        public ErrorPrototype PrototypeObject { get; }

        protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            return Construct(arguments, this);
        }

        public ObjectInstance Construct(JsValue[] arguments)
        {
            return Construct(arguments, this);
        }

        ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget) => Construct(arguments, newTarget);

        /// <summary>
        /// https://tc39.es/ecma262/#sec-nativeerror
        /// </summary>
        private ObjectInstance Construct(JsValue[] arguments, JsValue newTarget)
        {
            var o = OrdinaryCreateFromConstructor(
                newTarget,
                _intrinsicDefaultProto,
                static (Engine engine, Realm _, object? _) => new ErrorInstance(engine));

            var jsValue = arguments.At(0);
            if (!jsValue.IsUndefined())
            {
                var msg = TypeConverter.ToString(jsValue);
                var msgDesc = new PropertyDescriptor(msg, true, false, true);
                o.DefinePropertyOrThrow("message", msgDesc);
            }

            var stackString = BuildStackString();
            var stackDesc = new PropertyDescriptor(stackString, PropertyFlag.NonEnumerable);
            o.DefinePropertyOrThrow(CommonProperties.Stack, stackDesc);

            var options = arguments.At(1);

            o.InstallErrorCause(options);

            return o;

            JsValue BuildStackString()
            {
                var lastSyntaxNode = _engine.GetLastSyntaxElement();
                if (lastSyntaxNode == null)
                    return Undefined;

                var callStack = _engine.CallStack;
                var currentFunction = callStack.TryPeek(out var element) ? element.Function : null;

                // If the current function is the ErrorConstructor itself (i.e. "throw new Error(...)" was called
                // from script), exclude it from the stack trace, because the trace should begin at the throw point.
                return callStack.BuildCallStackString(lastSyntaxNode.Location, currentFunction == this ? 1 : 0);
            }
        }
    }
}
