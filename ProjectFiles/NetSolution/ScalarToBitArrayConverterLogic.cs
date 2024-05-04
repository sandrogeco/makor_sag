#region Using directives
using System;
using FTOptix.Core;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.OPCUAServer;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.OPCUAClient;
using FTOptix.Modbus;
using FTOptix.SQLiteStore;
using FTOptix.S7TiaProfinet;
using FTOptix.Recipe;
#endregion

public class ScalarToBitArrayConverterLogic : BaseNetLogic
{
    public override void Start()
    {
        var context = LogicObject.Context;
        logicObjectAffinityId = context.AssignAffinityId();
        logicObjectSenderId = context.AssignSenderId();
        arrayVariable = LogicObject.Owner.GetVariable("ArrayVariable");
        scalarVariable = LogicObject.Owner.GetVariable("ScalarVariable");

        VerifyVariables();

        variableSynchronizer = new RemoteVariableSynchronizer();
        variableSynchronizer.Add(new IUAVariable[] { scalarVariable });

        using (var resumeDispatchOnDispose = context.SuspendDispatch(logicObjectAffinityId))
        {
            arrayVariableValueChangeObserver = new CallbackVariableChangeObserver(ArrayVariableValueChanged);
            scalarVariableValueChangeObserver = new CallbackVariableChangeObserver(ScalarVariableValueChanged);

            arrayVariableRegistration = arrayVariable.RegisterEventObserver(
                arrayVariableValueChangeObserver, EventType.VariableValueChanged, logicObjectAffinityId);

            scalarVariableRegistration = scalarVariable.RegisterEventObserver(
                scalarVariableValueChangeObserver, EventType.VariableValueChanged, logicObjectAffinityId);

            ForeachBit(scalarVariable.Value.Value, SetBitInArrayVariable);
        }
    }

    public override void Stop()
    {
        using (var destroyDispatchOnDispose = LogicObject.Context.TerminateDispatchOnStop(logicObjectAffinityId))
        {
            if (variableSynchronizer != null)
            {
                variableSynchronizer.Dispose();
                variableSynchronizer = null;
            }

            if (scalarVariableRegistration != null)
            {
                scalarVariableRegistration.Dispose();
                scalarVariableRegistration = null;
            }

            if (arrayVariableRegistration != null)
            {
                arrayVariableRegistration.Dispose();
                arrayVariableRegistration = null;
            }

            scalarVariableValueChangeObserver = null;
            arrayVariableValueChangeObserver = null;
            scalarVariable = null;
            arrayVariable = null;
            logicObjectSenderId = 0;
            logicObjectAffinityId = 0;
        }
    }

    private void ArrayVariableValueChanged(IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId)
    {
        if (senderId == logicObjectSenderId)
            return;

        if (indexes != null && indexes.Length > 0)
        {
            using (var restorePreviousSenderIdOnDispose = LogicObject.Context.SetCurrentThreadSenderId(logicObjectSenderId))
            {
                var scalarValue = scalarVariable.Value.Value;
                SetBit(ref scalarValue, (int)indexes[0], (bool)newValue);
                scalarVariable.Value = new UAValue(scalarValue);
            }
        }
        else
            throw new NotImplementedException("Changes to the entire value of the ArrayVariable are not supported");
    }

    private void ScalarVariableValueChanged(IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId)
    {
        if (senderId == logicObjectSenderId)
            return;

        using (var restorePreviousSenderIdOnDispose = LogicObject.Context.SetCurrentThreadSenderId(logicObjectSenderId))
        {
            ForeachBitChanged(oldValue.Value, newValue.Value, SetBitInArrayVariable);
        }
    }

    private void VerifyVariables()
    {
        if (arrayVariable == null)
            throw new ArgumentException("ArrayVariable not found");

        if (scalarVariable == null)
            throw new ArgumentException("ScalarVariable not found");

        if (arrayVariable.DataType != OpcUa.DataTypes.Boolean)
            throw new ArgumentException("ArrayVariable must be a one-dimensional boolean array");

        if (arrayVariable.ArrayDimensions.Length != 1)
            throw new ArgumentException("ArrayVariable must be a one-dimensional boolean array");

        var scalarDataTypeId = scalarVariable.DataType;
        var arrayLength = arrayVariable.ArrayDimensions[0];

        if (scalarDataTypeId == OpcUa.DataTypes.SByte || scalarDataTypeId == OpcUa.DataTypes.Byte)
        {
            if (arrayLength != 8)
                throw new ArgumentException("ArrayVariable and ScalarVariable have incompatible dimensions");
        }
        else if (scalarDataTypeId == OpcUa.DataTypes.Int16 || scalarDataTypeId == OpcUa.DataTypes.UInt16)
        {
            if (arrayLength != 16)
                throw new ArgumentException("ArrayVariable and ScalarVariable have incompatible dimensions");
        }
        else if (scalarDataTypeId == OpcUa.DataTypes.Int32 || scalarDataTypeId == OpcUa.DataTypes.UInt32)
        {
            if (arrayLength != 32)
                throw new ArgumentException("ArrayVariable and ScalarVariable have incompatible dimensions");
        }
        else if (scalarDataTypeId == OpcUa.DataTypes.Int64 || scalarDataTypeId == OpcUa.DataTypes.UInt64)
        {
            if (arrayLength != 64)
                throw new ArgumentException("ArrayVariable and ScalarVariable have incompatible dimensions");
        }
        else
            throw new ArgumentException("ScalarVariable must be a concrete subtype of Integer or UInteger");
    }

    private void ForeachBit(object value, Action<int, bool> bitAction)
    {
        var numberOfBits = SizeOf(value);

        if (value is sbyte)
            ForeachBit((sbyte)value, numberOfBits, bitAction);
        else if (value is short)
            ForeachBit((short)value, numberOfBits, bitAction);
        else if (value is int)
            ForeachBit((int)value, numberOfBits, bitAction);
        else if (value is long)
            ForeachBit((long)value, numberOfBits, bitAction);
        else if (value is byte)
            ForeachBit((byte)value, numberOfBits, bitAction);
        else if (value is ushort)
            ForeachBit((ushort)value, numberOfBits, bitAction);
        else if (value is uint)
            ForeachBit((uint)value, numberOfBits, bitAction);
        else if (value is ulong)
            ForeachBit((long)(ulong)value, numberOfBits, bitAction);
        else
            throw new ArgumentException("Value argument must be an integer or uinteger");
    }

    private void ForeachBit(long number, int numberOfBits, Action<int, bool> bitAction)
    {
        for (int i = 0; i < numberOfBits; ++i)
        {
            var flag = GetBit(number, i);
            bitAction(i, flag);
        }
    }

    private void ForeachBitChanged(object oldValue, object newValue, Action<int, bool> onBitChangedAction)
    {
        var numberOfBits = SizeOf(oldValue);

        if (oldValue is sbyte)
            ForeachBitChanged((sbyte)oldValue, (sbyte)newValue, numberOfBits, onBitChangedAction);
        else if (oldValue is short)
            ForeachBitChanged((short)oldValue, (short)newValue, numberOfBits, onBitChangedAction);
        else if (oldValue is int)
            ForeachBitChanged((int)oldValue, (int)newValue, numberOfBits, onBitChangedAction);
        else if (oldValue is long)
            ForeachBitChanged((long)oldValue, (long)newValue, numberOfBits, onBitChangedAction);
        else if (oldValue is byte)
            ForeachBitChanged((byte)oldValue, (byte)newValue, numberOfBits, onBitChangedAction);
        else if (oldValue is ushort)
            ForeachBitChanged((ushort)oldValue, (ushort)newValue, numberOfBits, onBitChangedAction);
        else if (oldValue is uint)
            ForeachBitChanged((uint)oldValue, (uint)newValue, numberOfBits, onBitChangedAction);
        else if (oldValue is ulong)
            ForeachBitChanged((long)(ulong)oldValue, (long)(ulong)newValue, numberOfBits, onBitChangedAction);
        else
            throw new ArgumentException("OldValue and NewValue arguments must be an integer or uinteger");
    }

    private void ForeachBitChanged(long oldNumber, long newNumber, int numberOfBits, Action<int, bool> onBitChangedAction)
    {
        for (int i = 0; i < numberOfBits; ++i)
        {
            var oldFlag = GetBit(oldNumber, i);
            var newFlag = GetBit(newNumber, i);
            if (oldFlag != newFlag)
                onBitChangedAction(i, newFlag);
        }
    }

    private void SetBit(ref object value, int index, bool flag)
    {
        if (value is sbyte)
        {
            var number = (long)(sbyte)value;
            SetBit(ref number, index, flag);
            value = (sbyte)number;
        }
        else if (value is short)
        {
            var number = (long)(short)value;
            SetBit(ref number, index, flag);
            value = (short)number;
        }
        else if (value is int)
        {
            var number = (long)(int)value;
            SetBit(ref number, index, flag);
            value = (int)number;
        }
        else if (value is long)
        {
            var number = (long)value;
            SetBit(ref number, index, flag);
            value = number;
        }
        else if (value is byte)
        {
            var number = (long)(byte)value;
            SetBit(ref number, index, flag);
            value = (byte)number;
        }
        else if (value is ushort)
        {
            var number = (long)(ushort)value;
            SetBit(ref number, index, flag);
            value = (ushort)number;
        }
        else if (value is uint)
        {
            var number = (long)(uint)value;
            SetBit(ref number, index, flag);
            value = (uint)number;
        }
        else if (value is ulong)
        {
            var number = (long)(ulong)value;
            SetBit(ref number, index, flag);
            value = (ulong)number;
        }
        else
            throw new ArgumentException("Value argument must be an integer or uinteger");
    }

    private int SizeOf(object value)
    {
        if (value is sbyte || value is byte)
            return 8;
        if (value is short || value is ushort)
            return 16;
        if (value is int || value is uint)
            return 32;
        if (value is long || value is ulong)
            return 64;

        throw new ArgumentException("Value argument must be an integer or uinteger");
    }

    private bool GetBit(long number, int index)
    {
        return (number & (1 << index)) != 0;
    }

    private void SetBit(ref long number, int index, bool flag)
    {
        if (flag)
            number |= (1L << index);
        else
            number &= (~(1L << index));
    }

    private void SetBitInArrayVariable(int index, bool flag)
    {
        arrayVariable.SetValue(flag, new uint[] { (uint)index });
    }

    uint logicObjectAffinityId;
    ulong logicObjectSenderId;

    IUAVariable arrayVariable;
    IUAVariable scalarVariable;

    IEventObserver arrayVariableValueChangeObserver;
    IEventObserver scalarVariableValueChangeObserver;
    IEventRegistration arrayVariableRegistration;
    IEventRegistration scalarVariableRegistration;

    RemoteVariableSynchronizer variableSynchronizer;
}
