using WorldConfiguration = RTWServer.Game.World.Implementation.WorldConfiguration;

namespace RTWServer.Game.World.Interface;

public interface IWorldFactory
{
    IWorld CreateWorld();
    
    IWorld CreateWorld(WorldConfiguration config);
}
