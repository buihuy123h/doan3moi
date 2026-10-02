import { writable, derived, get } from 'svelte/store';
import { en, type Translations } from '../locales/en';
import { vi } from '../locales/vi';

export type Language = 'en' | 'vi';

function createLanguageStore() {
  const initial = ((): Language => {
    const saved = localStorage.getItem('nexus_language') as Language | null;
    return saved === 'vi' || saved === 'en' ? saved : 'en';
  })();

  const language = writable<Language>(initial);

  language.subscribe((lang) => {
    localStorage.setItem('nexus_language', lang);
    if (typeof document !== 'undefined') {
      document.documentElement.lang = lang;
    }
  });

  const setLanguage = (lang: Language) => language.set(lang);
  const toggleLanguage = () => language.update((prev) => (prev === 'en' ? 'vi' : 'en'));

  // Derived translation object
  const t = derived(language, ($language) => ($language === 'vi' ? vi : en));

  return { language, setLanguage, toggleLanguage, t };
}

export const languageStore = createLanguageStore();
