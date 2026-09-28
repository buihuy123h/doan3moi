import { writable } from 'svelte/store';

export type Theme = 'light' | 'dark';

function createThemeStore() {
  const initial = ((): Theme => {
    if (typeof window === 'undefined') return 'light';
    const saved = localStorage.getItem('nexus_theme') as Theme | null;
    if (saved === 'light' || saved === 'dark') return saved;
    return 'light'; // Default to light mode (#E0F1FF)
  })();

  const theme = writable<Theme>(initial);

  theme.subscribe((t) => {
    if (typeof document !== 'undefined') {
      const root = document.documentElement;
      if (t === 'dark') {
        root.classList.add('dark');
        root.style.colorScheme = 'dark';
      } else {
        root.classList.remove('dark');
        root.style.colorScheme = 'light';
      }
    }
    localStorage.setItem('nexus_theme', t);
  });

  const toggleTheme = () => theme.update((prev) => (prev === 'dark' ? 'light' : 'dark'));
  const setTheme = (newTheme: Theme) => theme.set(newTheme);

  return { theme, toggleTheme, setTheme };
}

export const themeStore = createThemeStore();
