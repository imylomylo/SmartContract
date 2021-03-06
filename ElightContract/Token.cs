﻿using System;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System.Numerics;
using System.ComponentModel;

namespace ElightContract
{
    //Elight coin token, it's used to create deposits in contracts between clients and carriers
    public static class Token
    {
        public static string Name() => "ElightCoin";
        public static string Symbol() => "EC";
        public static BigInteger TotalSupply() => 10000000;
        public static BigInteger Decimals() => 100000000;
        private static readonly byte[] NeoAssetId = {
            155, 124, 255, 218,
            166, 116, 190, 174,
            15, 147, 14, 190,
            96, 133, 175, 144,
            147, 229, 254, 86,
            179, 74, 92, 34,
            12, 205, 207, 110,
            252, 51, 111, 197
        };
        private const UInt64 SwapRate = 10;
        
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (!Runtime.CheckWitness(from))
            {
                Runtime.Notify("Invalid witness");
                return false;
            }
            
            if (value <= 0)
            {
                Runtime.Notify("Invalid value");
                return false;
            }
            
            if (from == to)
            {
                Runtime.Notify("Transfered");
                return true;
            }
            
            BigInteger fromValue = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (fromValue < value)
            {
                return false;
            }
            
            Storage.Put(Storage.CurrentContext, from, fromValue - value);
            BigInteger toValue = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, toValue + value);
            Runtime.Notify("Transfered");
            return true;
        }

        //it's forced because there is no checkwitness function in it
        public static void ForceAdd(byte[] from, BigInteger amount)
        {
            BigInteger fromValue = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            Storage.Put(Storage.CurrentContext, from, fromValue + amount);
        }

        //it's forced because there is no checkwitness function in it
        public static void ForceSub(byte[] from, BigInteger amount)
        {
            BigInteger fromValue = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            Storage.Put(Storage.CurrentContext, from, fromValue - amount);
        }

        public static bool MintTokens()
        {
            string senderSH = GetSender().AsString();

            if (senderSH.Length == 0)
            {
                return false;
            }
                
            BigInteger value = GetContributeValue();
            if (value == 0)
            {
                return false;
            }
                
            byte[] ba = Storage.Get(Storage.CurrentContext, senderSH);

            BigInteger balance;

            if (ba.Length == 0)
            {
                balance = 0;
            }
            else
            {
                balance = ba.AsBigInteger();
            }
            
            BigInteger token = value / Decimals();
            token = token * SwapRate;
            Storage.Put(Storage.CurrentContext, senderSH, balance + token);

            Runtime.Notify("Minted tokens");
            return true;
        }

        private static byte[] GetSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == NeoAssetId)
                {
                    return output.ScriptHash;
                }
            }
            return new byte[0];
        }

        public static BigInteger BalanceOf(byte[] address)
        {
            BigInteger currentBalance = Storage.Get(Storage.CurrentContext, address).AsBigInteger();
            return currentBalance;
        }

        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }

        private static UInt64 GetContributeValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            UInt64 value = 0;
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId == NeoAssetId)
                {
                    value += (UInt64)output.Value;
                }
            }
            return value;
        }
    }
}
