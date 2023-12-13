using Microsoft.EntityFrameworkCore;

namespace EFCoreReadWriteSeparate.DBModel
{
    public static class DbContextExtend
    {
        public static DbContext ToRead(this DbContext dbContext)
        {
            if (dbContext is MyDBContext)
                return ((MyDBContext)dbContext).ToRead();
            else
                throw new Exception();
        }

        public static DbContext ToWrite(this DbContext dbContext)
        {
            if (dbContext is MyDBContext)
                return ((MyDBContext)dbContext).ToWrite();
            else
                throw new Exception();

        }
    }
}
