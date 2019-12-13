using System;

namespace StorageService.Api.Entities
{
    /// <summary>
    /// 存储.
    /// </summary>
    public class Storage
    {
        /// <summary>
        /// 产品ID.
        /// </summary>
        public Guid ProductID { get; set; }

        /// <summary>
        /// 数量.
        /// </summary>
        public int Amount { get; set; }
    }
}