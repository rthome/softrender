using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftRender
{
    class Program
    {
        static int Main(string[] args)
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                SDL.SDL_LogError(SDL.SDL_LOG_CATEGORY_APPLICATION, "Couldn't initialize SDL: %s", __arglist(SDL.SDL_GetError()));
                return 1;
            }

            var window = SDL.SDL_CreateWindow("SoftRender", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, 640, 480, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            if (window == IntPtr.Zero)
            {
                SDL.SDL_LogError(SDL.SDL_LOG_CATEGORY_APPLICATION, "Couldn't create window: %s", __arglist(SDL.SDL_GetError()));
                SDL.SDL_Quit();
                return 2;
            }

            var renderer = SDL.SDL_CreateRenderer(window, -1, 0);
            if (renderer == IntPtr.Zero)
            {
                SDL.SDL_LogError(SDL.SDL_LOG_CATEGORY_APPLICATION, "Couldn't create renderer: %s", __arglist(SDL.SDL_GetError()));
                SDL.SDL_Quit();
                return 3;
            }

            var texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 640, 480);
            if (texture == IntPtr.Zero)
            {
                SDL.SDL_LogError(SDL.SDL_LOG_CATEGORY_APPLICATION, "Couldn't set create texture: %s", __arglist(SDL.SDL_GetError()));
                SDL.SDL_Quit();
                return 4;
            }

            bool done = false;
            while (!done)
            {
                SDL.SDL_Event e;
                while (SDL.SDL_PollEvent(out e) != 0)
                {
                    switch (e.type)
                    {
                        case SDL.SDL_EventType.SDL_KEYDOWN:
                            if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
                                done = true;
                            break;
                        case SDL.SDL_EventType.SDL_QUIT:
                            done = true;
                            break;
                    }

                    SDL.SDL_RenderClear(renderer);
                    SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
                    SDL.SDL_RenderPresent(renderer);
                }
            }

            SDL.SDL_DestroyRenderer(renderer);

            return 0;
        }
    }
}
