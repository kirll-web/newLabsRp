# Тесты для RankCalculator

## Unit-тесты

1. **CalculateRank_EmptyText_ReturnsZero**
    - Вход: id с пустым текстом в Redis
    - Ожидаемый результат: 0

2. **CalculateRank_AllAlphabetic_ReturnsZero**
    - Вход: текст только из букв ("abc")
    - Ожидаемый результат: 0

3. **CalculateRank_AllNonAlphabetic_ReturnsOne**
    - Вход: текст без букв ("123!@#")
    - Ожидаемый результат: 1

4. **CalculateRank_MixedText_ReturnsCorrectRatio**
    - Вход: текст "a1b2" (2 буквы из 4)
    - Ожидаемый результат: 0.5

5. **IsAlphabetic_RussianLetters_ReturnsTrue**
    - Вход: кириллические буквы
    - Ожидаемый результат: true

6. **IsAlphabetic_SpecialChars_ReturnsFalse**
    - Вход: символы "@", "1", " "
    - Ожидаемый результат: false
