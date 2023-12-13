using Microsoft.EntityFrameworkCore;

namespace EFCoreReadWriteSeparate.DBModel
{
    public static class DbContextExtend
    {

        /// <summary>
        /// 只读
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static DbContext Slave(this DbContext dbContext)
        {
            if (dbContext is MyDBContext)
            {
                return ((MyDBContext)dbContext).Slave();
            }
            else
                throw new Exception();
        }
        /// <summary>
        /// 读写
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static DbContext Master(this DbContext dbContext)
        {
            if (dbContext is MyDBContext)
            {
                return ((MyDBContext)dbContext).Master();
            }
            else
                throw new Exception();
        }
    }
}
