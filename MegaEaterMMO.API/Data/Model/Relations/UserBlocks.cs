namespace MegaEaterMMO.API.Data.Model.Relations
{
    public class UserBlocks : UserRelation
    {
        // User A is the blocker, User B is the blockee.
        // No need for unique composite key, as we want to allow users to block eachother.
        // Also this is actually all the class needs, if this exists it meanss a block is active, we don't even need a boolean :D
    }
}
