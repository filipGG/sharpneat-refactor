﻿/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2019 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software; you can redistribute it and/or modify
 * it under the terms of The MIT License (MIT).
 *
 * You should have received a copy of the MIT License
 * along with SharpNEAT; if not, see https://opensource.org/licenses/MIT.
 */
using System.Collections.Generic;
using System.Diagnostics;
using SharpNeat.BlackBox;
using SharpNeat.Evaluation;

namespace SharpNeat.Tasks.BinaryThreeMultiplexer
{
    // TODO: Consider a variant on this evaluator that uses two outputs instead of one, i.e. 'false' and 'true' outputs;
    // (if both outputs are low or high then that's just an invalid response).

    /// <summary>
    /// Binary 3-Multiplexer task evaluator.
    /// 
    /// One binary input selects which of two other binary inputs to output. 
    /// The correct response is the selected input's input signal (0 or 1).
    ///
    /// Evaluation consists of querying the provided black box for all possible input combinations (2^3 = 8). 
    /// </summary>
    public class BinaryThreeMultiplexerEvaluator : IPhenomeEvaluator<IBlackBox<double>>
    {
        #region Properties

        /// <summary>
        /// Indicates if the evaluation scheme is deterministic, i.e. will always return the same fitness score for a given genome.
        /// </summary>
        /// <remarks>
        /// An evaluation scheme that has some random/stochastic characteristics may give a different fitness score at each invocation 
        /// for the same genome, such as scheme is non-deterministic.
        /// </remarks>
        public bool IsDeterministic => true;

        /// <summary>
        /// Gets a null fitness score, i.e. for genomes that cannot be assigned a fitness score for whatever reason, e.g.
        /// if a genome failed to decode to a viable phenome that could be tested.
        /// </summary>
        public FitnessInfo NullFitness => FitnessInfo.DefaultFitnessInfo;

        /// <summary>
        /// Gets a fitness comparer. 
        /// </summary>
        public IComparer<FitnessInfo> FitnessComparer => PrimaryFitnessInfoComparer.Singleton;

        /// <summary>
        /// True if this phenome evaluator uses evaluation state objects, i.e. <see cref="CreateEvaluationStateObject"/> returns
        /// an object (rather than a null reference), and one of these objects must be passed on each call to <see cref="Evaluate(IBlackBox{double}, object)"/>
        /// </summary>
        public bool UsesEvaluationStateObject => false;

        #endregion

        #region Public Methods

        /// <summary>
        /// Evaluate the provided IBlackBox against the Binary 11-Multiplexer problem domain and return
        /// its fitness score.
        /// </summary>
        /// <param name="box">The black box to evaluate.</param>
        /// <param name="evaluationState">Optional evaluation state object that can be re-used between evaluations.</param>
        public FitnessInfo Evaluate(IBlackBox<double> box, object evaluationState)
        {
            double fitness = 0.0;
            bool success = true;
            IVector<double> inputVec = box.InputVector;
            IVector<double> outputVec = box.OutputVector;
            
            // 8 test cases.
            for(int i=0; i < 8; i++)
            {
                // Bias input.
                inputVec[0] = 1.0;

                // Apply bitmask to i and shift left to generate the input signals.
                // Note. We could eliminate all the boolean logic by pre-building a table of test 
                // signals and correct responses.
                for(int tmp = i, j=1; j < 4; j++) 
                {   
                    inputVec[j] = tmp & 0x1;
                    tmp >>= 1;
                }
                                
                // Activate the black box.
                box.Activate();

                // Read output signal.
                double output = outputVec[0];
                Debug.Assert(output >= 0.0, "Unexpected negative output.");
                bool trueResponse = (output > 0.5);

                // Determine the correct answer with somewhat cryptic bit manipulation.
                // The condition is true if the correct answer is true (1.0).
                if(((1 << (1 + (i & 0x1))) &i) != 0)
                {   
                    // correct answer: true.
                    // Assign fitness on sliding scale between 0.0 and 1.0 based on squared error.
                    // In tests squared error drove evolution significantly more efficiently in this domain than absolute error.
                    // Note. To base fitness on absolute error use: fitness += output;
                    fitness += 1.0 - ((1.0 - output) * (1.0 - output));

                    // Reset success flag if at least one response is wrong.
                    success &= trueResponse;
                }
                else
                {   
                    // correct answer: false.
                    // Assign fitness on sliding scale between 0.0 and 1.0 based on squared error.
                    // In tests squared error drove evolution significantly more efficiently in this domain than absolute error.
                    // Note. To base fitness on absolute error use: fitness += 1.0-output;
                    fitness += 1.0 - (output * output);

                    // Reset success flag if at least one response is wrong.
                    success &= !trueResponse; 
                }

                // Reset black box ready for next test case.
                box.ResetState();
            }

            // If the correct answer was given in each case then add a bonus value to the fitness.
            if(success) {
                fitness += 100.0;
            }

            return new FitnessInfo(fitness);
        }

        /// <summary>
        /// Accepts a <see cref="FitnessInfo"/>, which is intended to be from the fittest genome in the population, and returns a boolean
        /// that indicates if the evolution algorithm can stop, i.e. because the fitness is the best that can be achieved (or good enough).
        /// </summary>
        /// <param name="fitnessInfo">The fitness info object to test.</param>
        /// <returns>Returns true if the fitness is good enough to signal the evolution algorithm to stop.</returns>
        public bool TestForStopCondition(FitnessInfo fitnessInfo)
        {
            return (fitnessInfo.PrimaryFitness >= 100);
        }

        /// <summary>
        /// Create an evaluation state object.
        /// </summary>
        /// <returns>A new instance of an evaluation state object for the current.</returns>
        public object CreateEvaluationStateObject()
        {
            return null;
        }

        #endregion
    }
}