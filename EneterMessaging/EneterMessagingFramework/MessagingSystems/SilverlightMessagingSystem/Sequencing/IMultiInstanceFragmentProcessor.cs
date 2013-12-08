/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System.Collections.Generic;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem.Sequencing
{
    /// <summary>
    /// The interface declares the fragment processor that is able to process more sequences in parallel.
    /// </summary>
    internal interface IMultiInstanceFragmentProcessor
    {
        /// <summary>
        /// Processes the fragment and returns the sequence of processed fragments ready for the user.
        /// If the processing of the fragment do not result in a sequence for the user it can return the empty sequence.<br/>
        /// 
        /// E.g.: It can happen the fragments of the sequence do not come in the right order. Therefore the processing
        ///       can hold such fragments and return the empty sequence. Then later when it is possible to return
        ///       fragments in the correct order the processing can return the sequence with fragments.
        /// </summary>
        IEnumerable<Fragment> ProcessFragment(Fragment fragment);
    }
}

#endif