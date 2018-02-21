﻿using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace ElightContract
{
    public class Elight : SmartContract
    {
        public static bool Invoke(string authorAddress, BigInteger i, byte[] arg)
        {
            if (!Runtime.CheckWitness(authorAddress.AsByteArray()))
            {
                Runtime.Notify("Invalid witness");
                return false;
            }

            Contract contract = Contract.GetContract(authorAddress, i);
            Runtime.Notify(contract.Conditions);
            Runtime.Notify("With deposit?");
            Runtime.Notify(contract.ContractOption == Contract.Option.WithDeposit);

            if (contract.Status != Contract.STATUS.ACTIVE)
            {
                Runtime.Notify("Already executed");
                return false;
            }
            
            byte[] source = contract.Conditions;

            Interpreter interpreter = Interpreter.Init();
            interpreter = Interpreter.Run(interpreter, contract, arg);

            Contract.STATUS status = Contract.STATUS.EXECUTION_ERROR;
            if (interpreter.isOk)
            {
                Int32 res = Interpreter.GetResult(interpreter);
                
                bool isConditionOk = res == 1;
                if (isConditionOk)
                {
                    status = Contract.STATUS.SUCCESS;
                    Runtime.Notify("SUCCESS");
                }
                else
                {
                    status = Contract.STATUS.FAILURE;
                    Runtime.Notify("FAILURE");
                }

                if (contract.ContractOption == Contract.Option.WithDeposit)
                {
                    Deposit.Unfreeze(contract.Deposit, isConditionOk);
                }
            }

            Contract.ChangeStatus(contract, authorAddress, status);
            return status != Contract.STATUS.EXECUTION_ERROR;
        }
        

        //05 0705
        //testinvoke script_hash add ["AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y","-26<(x+2)<26",b'000000027ffffffe0000001a7fffffff0000001a7ffffffa']
        //testinvoke script_hash invoke ["AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y",1,b'0000000f'] //true
        //testinvoke script_hash invoke ["AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y",1,b'0000001a'] //false
        public static object Main(string operation, params object[] args)
        {
            if (operation == "init")
            {
                Contract contract = Contract.Init((byte[])args[1], (byte[])args[2]);
                Contract.PutContract(contract, (string)args[0]);
            }
            if (operation == "initDeposit")
            {
                Contract contract = Contract.Init((byte[])args[1], (byte[])args[2]);
                Contract.InitDeposit(contract, (byte[])args[1], (byte[])args[2], (BigInteger)args[3]);
                Contract.PutContract(contract, (string)args[0]);
            }
            else if (operation == "invoke") 
            {
                Runtime.Notify(args[0]);
                Runtime.Notify(args[1]);
                Runtime.Notify(args[2]);
                return Invoke((string)args[0], (BigInteger)args[1], (byte[])args[2]);
            }
            else if (operation == "mint")
            {
                return Token.MintTokens();
            }
            else if (operation == "transfer")
            {
                byte[] from = (byte[])args[0];
                byte[] to = (byte[])args[1];
                BigInteger value = (BigInteger)args[2];
                return Token.Transfer(from, to, value);
            }
            else if (operation == "name")
            {
                return Token.Name();
            }
            else if (operation == "symbol")
            {
                return Token.Symbol();
            }
            else if (operation == "total")
            {
                return Token.TotalSupply();
            }
            else if (operation == "balanceOf")
            {
                return Token.BalanceOf((byte[])args[0]);
            }
            else if (operation == "decimals")
            {
                return Token.Decimals();
            }
            return true;
        }
    }
}