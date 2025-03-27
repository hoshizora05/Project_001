using System;
using System.Collections.Generic;
using UnityEngine;

namespace LifeResourceSystem
{
    public class FinanceManager
    {
        private FinanceSystem financeData;
        private LifeResourceConfig.FinanceConfig config;
        
        // Events
        public event Action<FinanceSystem.Transaction> OnTransactionProcessed;
        
        public FinanceManager(string playerId, LifeResourceConfig.FinanceConfig config)
        {
            this.config = config;
            
            financeData = new FinanceSystem
            {
                playerId = playerId,
                currentMoney = config.startingMoney,
                incomeSources = new List<FinanceSystem.IncomeSource>(config.defaultIncomeSources),
                expenses = new List<FinanceSystem.RecurringExpense>(config.defaultExpenses),
                transactions = new List<FinanceSystem.Transaction>(),
                dailyIncome = 0,
                dailyExpenses = 0,
                monthlyBalance = 0,
                financialStability = 50 // Default middle value
            };
            
            // Calculate initial financial metrics
            UpdateFinancialMetrics();
        }
        
        public bool ProcessTransaction(string transactionId, string description, float amount, string category)
        {
            // Check if we have enough money for expenses
            if (amount < 0 && financeData.currentMoney < Math.Abs(amount))
                return false;
                
            // Create transaction record
            var transaction = new FinanceSystem.Transaction
            {
                transactionId = transactionId,
                description = description,
                amount = amount,
                category = category,
                timestamp = Time.time,
                relatedEntityId = ""
            };
            
            // Apply transaction
            financeData.currentMoney += amount;
            financeData.transactions.Add(transaction);
            
            // Limit transaction history size
            if (financeData.transactions.Count > 100)
            {
                financeData.transactions.RemoveAt(0);
            }
            
            // Update metrics
            UpdateFinancialMetrics();
            
            // Fire event
            OnTransactionProcessed?.Invoke(transaction);
            
            return true;
        }
        
        public void ProcessTimeBasedTransactions(float currentTime)
        {
            // Process income
            foreach (var income in financeData.incomeSources)
            {
                if (income.isActive && currentTime >= income.nextPaymentTime)
                {
                    // Create transaction
                    var transaction = new FinanceSystem.Transaction
                    {
                        transactionId = $"income_{DateTime.Now.Ticks}",
                        description = $"Income: {income.sourceName}",
                        amount = income.amount,
                        category = "Income",
                        timestamp = currentTime,
                        relatedEntityId = income.sourceId
                    };
                    
                    // Apply income
                    financeData.currentMoney += income.amount;
                    financeData.transactions.Add(transaction);
                    
                    // Update next payment time
                    income.nextPaymentTime = CalculateNextPaymentTime(currentTime, income.frequency);
                    
                    // Fire event
                    OnTransactionProcessed?.Invoke(transaction);
                }
            }
            
            // Process expenses
            foreach (var expense in financeData.expenses)
            {
                if (currentTime >= expense.nextPaymentTime)
                {
                    bool paid = true;
                    
                    // Check if we can pay
                    if (financeData.currentMoney < expense.amount)
                    {
                        if (expense.isOptional)
                        {
                            // Skip optional payment if can't afford
                            paid = false;
                            
                            // TODO: Handle consequences
                        }
                        else
                        {
                            // Force payment, go into debt
                            paid = true;
                        }
                    }
                    
                    if (paid)
                    {
                        // Create transaction
                        var transaction = new FinanceSystem.Transaction
                        {
                            transactionId = $"expense_{DateTime.Now.Ticks}",
                            description = $"Expense: {expense.expenseName}",
                            amount = -expense.amount,
                            category = "Expense",
                            timestamp = currentTime,
                            relatedEntityId = expense.expenseId
                        };
                        
                        // Apply expense
                        financeData.currentMoney -= expense.amount;
                        financeData.transactions.Add(transaction);
                        
                        // Fire event
                        OnTransactionProcessed?.Invoke(transaction);
                    }
                    
                    // Update next payment time
                    expense.nextPaymentTime = CalculateNextPaymentTime(currentTime, expense.frequency);
                }
            }
            
            // Limit transaction history size
            if (financeData.transactions.Count > 100)
            {
                financeData.transactions.RemoveRange(0, financeData.transactions.Count - 100);
            }
            
            // Update metrics
            UpdateFinancialMetrics();
        }
        
        private float CalculateNextPaymentTime(float currentTime, FinanceSystem.PaymentFrequency frequency)
        {
            switch (frequency)
            {
                case FinanceSystem.PaymentFrequency.Daily:
                    return currentTime + 24f;
                case FinanceSystem.PaymentFrequency.Weekly:
                    return currentTime + 24f * 7f;
                case FinanceSystem.PaymentFrequency.BiWeekly:
                    return currentTime + 24f * 14f;
                case FinanceSystem.PaymentFrequency.Monthly:
                    return currentTime + 24f * 30f;
                case FinanceSystem.PaymentFrequency.Yearly:
                    return currentTime + 24f * 365f;
                case FinanceSystem.PaymentFrequency.OneTime:
                    return float.MaxValue; // One-time payments don't recur
                default:
                    return currentTime + 24f;
            }
        }
        
        public void ProcessDailyFinancials()
        {
            // Recalculate financial metrics
            UpdateFinancialMetrics();
        }
        
        private void UpdateFinancialMetrics()
        {
            // Calculate daily income
            financeData.dailyIncome = 0;
            foreach (var income in financeData.incomeSources)
            {
                if (income.isActive)
                {
                    switch (income.frequency)
                    {
                        case FinanceSystem.PaymentFrequency.Daily:
                            financeData.dailyIncome += income.amount;
                            break;
                        case FinanceSystem.PaymentFrequency.Weekly:
                            financeData.dailyIncome += income.amount / 7f;
                            break;
                        case FinanceSystem.PaymentFrequency.BiWeekly:
                            financeData.dailyIncome += income.amount / 14f;
                            break;
                        case FinanceSystem.PaymentFrequency.Monthly:
                            financeData.dailyIncome += income.amount / 30f;
                            break;
                        case FinanceSystem.PaymentFrequency.Yearly:
                            financeData.dailyIncome += income.amount / 365f;
                            break;
                    }
                }
            }
            
            // Calculate daily expenses
            financeData.dailyExpenses = 0;
            foreach (var expense in financeData.expenses)
            {
                switch (expense.frequency)
                {
                    case FinanceSystem.PaymentFrequency.Daily:
                        financeData.dailyExpenses += expense.amount;
                        break;
                    case FinanceSystem.PaymentFrequency.Weekly:
                        financeData.dailyExpenses += expense.amount / 7f;
                        break;
                    case FinanceSystem.PaymentFrequency.BiWeekly:
                        financeData.dailyExpenses += expense.amount / 14f;
                        break;
                    case FinanceSystem.PaymentFrequency.Monthly:
                        financeData.dailyExpenses += expense.amount / 30f;
                        break;
                    case FinanceSystem.PaymentFrequency.Yearly:
                        financeData.dailyExpenses += expense.amount / 365f;
                        break;
                }
            }
            
            // Calculate monthly balance
            financeData.monthlyBalance = (financeData.dailyIncome - financeData.dailyExpenses) * 30f;
            
            // Calculate financial stability (0-100)
            if (financeData.dailyExpenses <= 0)
            {
                // No expenses, very stable
                financeData.financialStability = 100;
            }
            else
            {
                // Months of runway at current rate
                float runway = financeData.currentMoney / financeData.dailyExpenses;
                
                // Monthly surplus or deficit ratio
                float monthlyRatio = financeData.monthlyBalance / (financeData.dailyExpenses * 30f);
                
                // Stability score calculation:
                // - Runway points (up to 50): More months of runway = more stability
                // - Monthly ratio points (up to 50): Higher income:expense ratio = more stability
                float runwayPoints = Mathf.Clamp(runway / 60f * 50f, 0f, 50f); // 2 months = 50/6 points, 6 months = 50/2 points, 12+ months = 50 points
                float ratioPoints = Mathf.Clamp((monthlyRatio + 1f) * 25f, 0f, 50f); // -1 ratio = 0 points, 0 ratio = 25 points, +1 ratio = 50 points
                
                financeData.financialStability = runwayPoints + ratioPoints;
            }
        }
        
        public float GetActionCost(string actionId)
        {
            if (config.actionCosts.TryGetValue(actionId, out float cost))
            {
                return cost;
            }
            return 0f;
        }
        
        public bool HasEnoughMoney(float amount)
        {
            return financeData.currentMoney >= amount;
        }
        
        public float GetCurrentMoney()
        {
            return financeData.currentMoney;
        }
        
        public float GetDailyExpenses()
        {
            return financeData.dailyExpenses;
        }
        
        public FinancialState GetFinancialState()
        {
            // Get recent transactions (last 10)
            List<FinanceSystem.Transaction> recentTransactions = new List<FinanceSystem.Transaction>();
            int transactionCount = financeData.transactions.Count;
            int startIndex = Math.Max(0, transactionCount - 10);
            
            for (int i = startIndex; i < transactionCount; i++)
            {
                recentTransactions.Add(financeData.transactions[i]);
            }
            
            return new FinancialState
            {
                currentMoney = financeData.currentMoney,
                dailyIncome = financeData.dailyIncome,
                dailyExpenses = financeData.dailyExpenses,
                monthlyBalance = financeData.monthlyBalance,
                financialStability = financeData.financialStability,
                recentTransactions = recentTransactions
            };
        }
        
        public FinanceSaveData GenerateSaveData()
        {
            return new FinanceSaveData
            {
                playerId = financeData.playerId,
                currentMoney = financeData.currentMoney,
                incomeSources = new List<FinanceSystem.IncomeSource>(financeData.incomeSources),
                expenses = new List<FinanceSystem.RecurringExpense>(financeData.expenses),
                recentTransactions = new List<FinanceSystem.Transaction>(financeData.transactions),
                dailyIncome = financeData.dailyIncome,
                dailyExpenses = financeData.dailyExpenses,
                monthlyBalance = financeData.monthlyBalance,
                financialStability = financeData.financialStability
            };
        }
        
        public void RestoreFromSaveData(FinanceSaveData saveData)
        {
            financeData.playerId = saveData.playerId;
            financeData.currentMoney = saveData.currentMoney;
            financeData.incomeSources = new List<FinanceSystem.IncomeSource>(saveData.incomeSources);
            financeData.expenses = new List<FinanceSystem.RecurringExpense>(saveData.expenses);
            financeData.transactions = new List<FinanceSystem.Transaction>(saveData.recentTransactions);
            financeData.dailyIncome = saveData.dailyIncome;
            financeData.dailyExpenses = saveData.dailyExpenses;
            financeData.monthlyBalance = saveData.monthlyBalance;
            financeData.financialStability = saveData.financialStability;
        }
    }
}